using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ganryujima.Input.Face
{
    internal sealed class FaceBridgeServer : IDisposable
    {
        private readonly FaceInput owner;
        private readonly string webDirectory, modelPath, pagePath;
        private readonly string token = Guid.NewGuid().ToString("N");
        private readonly SemaphoreSlim capacity = new SemaphoreSlim(FaceConfig.BridgeConcurrency);
        private HttpListener listener;
        private volatile bool stopped;
        public string Address { get; private set; }
        internal string Token => token;

        internal FaceBridgeServer(FaceInput owner, string directory, string pagePath)
        {
            this.owner = owner;
            this.pagePath = pagePath;
            webDirectory = Path.GetFullPath(Path.Combine(directory, "Web"));
            modelPath = Path.Combine(directory, "Models", "face_landmarker.task");
        }

        internal void Start(int port)
        {
            Exception failure = null;
            for (int attempt = 0; attempt < FaceConfig.BridgePortAttempts; attempt++)
            {
                try
                {
                    Address = "http://127.0.0.1:" + (port + attempt) + "/";
                    listener = new HttpListener();
                    listener.Prefixes.Add(Address);
                    listener.Start();
                    _ = Accept();
                    return;
                }
                catch (Exception error) { failure = error; listener?.Close(); }
            }
            throw new InvalidOperationException("Could not start the localhost face bridge", failure);
        }

        private async Task Accept()
        {
            try
            {
                while (!stopped)
                {
                    HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                    if (!capacity.Wait(0)) { context.Response.StatusCode = 503; context.Response.Close(); continue; }
                    _ = Handle(context);
                }
            }
            catch (Exception error) { if (!stopped) owner.ReportFailure("Face bridge stopped: " + error.Message); }
        }

        private async Task Handle(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
                response.Headers["Permissions-Policy"] = "camera=(self), microphone=()";
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; worker-src 'self' blob:; style-src 'self'; img-src 'self' blob: data:; media-src 'self' blob:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'";
                if (!string.Equals(request.UserHostName, new Uri(Address).Authority, StringComparison.OrdinalIgnoreCase)) { response.StatusCode = 403; return; }
                string origin = request.Headers["Origin"];
                if (origin != null && !string.Equals(origin, Address.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) { response.StatusCode = 403; return; }
                string path = request.Url.AbsolutePath;
                if (request.HttpMethod == "GET" && (path == "/" || path == "/face-test.html"))
                {
                    if (!File.Exists(pagePath)) { await Text(response, 404, "Missing public/face-test.html"); return; }
                    await Text(response, 200, File.ReadAllText(pagePath).Replace("__FACE_TOKEN__", token), "text/html; charset=utf-8");
                    return;
                }
                if (request.HttpMethod == "GET" && path == "/face/config.js")
                {
                    await Text(response, 200, "export const FACE_CONFIG = " + JsonUtility.ToJson(new BrowserConfig()) + ";", "text/javascript");
                    return;
                }
                if (request.HttpMethod == "GET" && (path.StartsWith("/face/", StringComparison.Ordinal) || path == "/models/face_landmarker.task"))
                {
                    string file = path == "/models/face_landmarker.task" ? modelPath : Path.GetFullPath(Path.Combine(webDirectory, Uri.UnescapeDataString(path.Substring(6)).Replace('/', Path.DirectorySeparatorChar)));
                    if (path != "/models/face_landmarker.task" && !file.StartsWith(webDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { response.StatusCode = 403; return; }
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    string mime = extension == ".wasm" ? "application/wasm" : extension == ".js" || extension == ".mjs" ? "text/javascript" : extension == ".css" ? "text/css" : extension == ".task" ? "application/octet-stream" : null;
                    if (mime == null || !File.Exists(file)) { await Text(response, 404, "Missing local face asset. Run the face setup script."); return; }
                    byte[] bytes = File.ReadAllBytes(file);
                    response.ContentType = mime;
                    response.ContentLength64 = bytes.Length;
                    await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    return;
                }
                if (!string.Equals(request.Headers["X-Face-Token"], token, StringComparison.Ordinal)) { response.StatusCode = 403; return; }
                if (request.HttpMethod == "GET" && path == "/state") { await Text(response, 200, owner.StatusJson, "application/json"); return; }
                if (request.HttpMethod != "POST") { response.StatusCode = 405; return; }
                byte[] body = await ReadLimited(request).ConfigureAwait(false);
                switch (path)
                {
                    case "/sample":
                        FaceSample sample = JsonUtility.FromJson<FaceSample>(Encoding.UTF8.GetString(body));
                        if (sample == null || (sample.tracking && !sample.IsValid()) || (sample.landmarks != null && sample.landmarks.Length > FaceConfig.MaximumLandmarkValues)) { response.StatusCode = 400; return; }
                        owner.Enqueue(sample);
                        break;
                    case "/status":
                        BrowserStatus status = JsonUtility.FromJson<BrowserStatus>(Encoding.UTF8.GetString(body));
                        if (status == null) { response.StatusCode = 400; return; }
                        owner.ReportBrowser(status.ready, status.error);
                        break;
                    case "/preview":
                        if (body.Length < 4 || body[0] != 0xff || body[1] != 0xd8) { response.StatusCode = 400; return; }
                        owner.ReceivePreview(body);
                        break;
                    case "/calibrate": owner.RequestCalibration(); break;
                    default: response.StatusCode = 404; return;
                }
                await Text(response, 200, "{\"ok\":true}", "application/json");
            }
            catch (InvalidDataException) { context.Response.StatusCode = 413; }
            catch (Exception) { try { context.Response.StatusCode = 400; } catch (ObjectDisposedException) { } }
            finally
            {
                try { context.Response.Close(); } catch (Exception) { }
                capacity.Release();
            }
        }

        private static async Task<byte[]> ReadLimited(HttpListenerRequest request)
        {
            if (request.ContentLength64 > FaceConfig.MaximumRequestBytes) throw new InvalidDataException();
            using (var result = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                while (true)
                {
                    Task<int> read = request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                    if (await Task.WhenAny(read, Task.Delay(FaceConfig.RequestTimeoutMs)).ConfigureAwait(false) != read) throw new InvalidDataException("Request timed out");
                    int count = await read.ConfigureAwait(false);
                    if (count == 0) break;
                    if (result.Length + count > FaceConfig.MaximumRequestBytes) throw new InvalidDataException();
                    result.Write(buffer, 0, count);
                }
                return result.ToArray();
            }
        }

        private static async Task Text(HttpListenerResponse response, int status, string text, string type = "text/plain; charset=utf-8")
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            response.StatusCode = status;
            response.ContentType = type;
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        public void Dispose()
        {
            stopped = true;
            try { listener?.Close(); } catch (Exception) { }
        }

        [Serializable] private sealed class BrowserStatus { public bool ready = false; public string error = ""; }
        [Serializable] private sealed class BrowserConfig
        {
            public int detectionHz = FaceConfig.DetectionHz;
            public int requestTimeoutMs = FaceConfig.RequestTimeoutMs;
            public int modelLoadTimeoutMs = FaceConfig.ModelLoadTimeoutMs;
            public int eventFlashMs = FaceConfig.EventFlashMs;
            public int webcamWidth = FaceConfig.WebcamWidth, webcamHeight = FaceConfig.WebcamHeight;
            public float detectionConfidence = FaceConfig.MinimumDetectionConfidence;
            public float presenceConfidence = FaceConfig.MinimumPresenceConfidence;
            public float trackingConfidence = FaceConfig.MinimumTrackingConfidence;
            public float yawSign = FaceConfig.PoseYawSign, pitchSign = FaceConfig.PosePitchSign, rollSign = FaceConfig.PoseRollSign;
            public int previewHz = FaceConfig.PreviewHz, previewWidth = FaceConfig.PreviewWidth;
            public float previewQuality = FaceConfig.PreviewJpegQuality;
        }
    }
}