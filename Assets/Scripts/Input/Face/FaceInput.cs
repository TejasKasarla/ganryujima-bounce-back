using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ganryujima.Input.Face
{
    /// <summary>Receives local browser MediaPipe samples; never performs inference on Unity's render thread.</summary>
    public sealed class FaceInput : IFaceInput
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Queue<ReceivedSample> pending = new Queue<ReceivedSample>();
        private readonly object gate = new object();
        private readonly string directory, pagePath;
        private readonly bool openBrowser;
        private readonly int port;
        private FaceSignalProcessor processor = new FaceSignalProcessor();
        private FaceBridgeServer server;
        private TaskCompletionSource<bool> calibration;
        private volatile bool running, ready, failed, previewRequested, heartbeatExpired;
        private volatile string error = "", statusJson = "{}";
        private long heartbeat;
        private int calibrationRequested;
        private bool autoCalibrate = true;
        private double nextPublish;
        private byte[] preview;
        private float[] landmarks;
        private EventTotals events = new EventTotals();

        public bool IsReady => running && ready && !failed;
        public bool HasFailed => failed;
        public bool IsRunning => running;
        public string Error => error;
        public string Address => server?.Address;
        public bool IsCalibrating => processor.IsCalibrating;
        public double CalibrationRemainingMs => processor.CalibrationRemainingMs;
        public string CalibrationStatus => processor.CalibrationStatus;
        public byte[] PreviewFrame => preview;
        public float[] PreviewLandmarks => landmarks;
        public bool PreviewRequested { get => previewRequested; set => previewRequested = value; }
        internal string StatusJson => statusJson;
        internal string Token => server?.Token;

        public FaceInput() : this(DefaultDirectory(), DefaultPage(), FaceConfig.BridgePort, true) { }

        internal FaceInput(string directory, string pagePath, int port, bool openBrowser)
        {
            this.directory = directory;
            this.pagePath = pagePath;
            this.port = port;
            this.openBrowser = openBrowser;
        }

        public Task start()
        {
            try
            {
                if (running) return Task.CompletedTask;
                failed = ready = heartbeatExpired = false;
                error = "";
                processor = new FaceSignalProcessor();
                events = new EventTotals();
                autoCalibrate = true;
                running = true;
                server = new FaceBridgeServer(this, directory, pagePath);
                server.Start(port);
                Publish();
                if (openBrowser) Application.OpenURL(server.Address);
            }
            catch (Exception exception)
            {
                server?.Dispose();
                running = false;
                ReportFailure("Camera unavailable: " + exception.Message);
            }
            return Task.CompletedTask;
        }

        public void stop()
        {
            running = ready = false;
            server?.Dispose();
            server = null;
            lock (gate) pending.Clear();
            processor.CancelCalibration();
            processor = new FaceSignalProcessor();
            calibration?.TrySetResult(false);
            calibration = null;
            preview = null;
            landmarks = null;
            Publish();
        }

        public Task calibrate(int durationMs = FaceConfig.CalibrationDurationMs)
        {
            calibration?.TrySetResult(false);
            if (!running || failed) return Task.CompletedTask;
            calibration = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            processor.BeginCalibration(clock.Elapsed.TotalMilliseconds, durationMs);
            autoCalibrate = false;
            return calibration.Task;
        }

        public void tick()
        {
            double now = clock.Elapsed.TotalMilliseconds;
            processor.BeginFrame(now);
            if (running)
            {
                if (Interlocked.Exchange(ref calibrationRequested, 0) != 0) _ = calibrate();
                while (true)
                {
                    ReceivedSample received;
                    lock (gate)
                    {
                        if (pending.Count == 0) break;
                        received = pending.Dequeue();
                    }
                    if (autoCalibrate && received.sample.IsValid()) { _ = calibrate(); autoCalibrate = false; }
                    processor.Process(received.sample, received.time);
                    if (received.sample.tracking) landmarks = received.sample.landmarks;
                }
                if (ready && now - Interlocked.Read(ref heartbeat) > FaceConfig.BrowserHeartbeatTimeoutMs)
                {
                    ReportFailure("Tracker browser disconnected; keyboard fallback active");
                    heartbeatExpired = true;
                }
            }
            processor.EndFrame(now);
            FaceState snapshot = processor.State;
            if (snapshot.leanTrigger != 0) events.lean++;
            if (snapshot.kiai) events.kiai++;
            if (snapshot.nod) events.nod++;
            if (snapshot.shake) events.shake++;
            if (!processor.IsCalibrating && calibration != null) { calibration.TrySetResult(processor.State.calibrated); calibration = null; }
            if (now >= nextPublish) { Publish(); nextPublish = now + 1000.0 / FaceConfig.DetectionHz; }
        }

        public FaceState getState() => processor.State;

        internal void Enqueue(FaceSample sample)
        {
            if (!running) return;
            Interlocked.Exchange(ref heartbeat, (long)clock.Elapsed.TotalMilliseconds);
            if (heartbeatExpired)
            {
                heartbeatExpired = false;
                failed = false;
                ready = true;
                error = "";
            }
            lock (gate)
            {
                while (pending.Count >= FaceConfig.MaximumPendingSamples) pending.Dequeue();
                pending.Enqueue(new ReceivedSample { sample = sample, time = clock.Elapsed.TotalMilliseconds });
            }
        }

        internal void ReportBrowser(bool cameraReady, string message)
        {
            heartbeatExpired = false;
            Interlocked.Exchange(ref heartbeat, (long)clock.Elapsed.TotalMilliseconds);
            if (!string.IsNullOrEmpty(message)) { ReportFailure(message); return; }
            ready = cameraReady;
            if (cameraReady) { failed = false; error = ""; }
        }

        internal void ReportFailure(string message)
        {
            failed = true;
            ready = false;
            error = string.IsNullOrEmpty(message) ? "Camera unavailable" : message.Substring(0, Math.Min(message.Length, 500));
        }

        internal void ReceivePreview(byte[] bytes) { if (running && previewRequested) preview = bytes; }
        internal void RequestCalibration() => Interlocked.Exchange(ref calibrationRequested, 1);

        private void Publish()
        {
            statusJson = JsonUtility.ToJson(new Status
            {
                state = processor.State,
                events = events,
                ready = IsReady,
                error = error,
                calibrating = processor.IsCalibrating,
                remainingMs = processor.CalibrationRemainingMs,
                calibrationStatus = processor.CalibrationStatus,
                previewRequested = previewRequested
            });
        }

        private static string DefaultDirectory()
        {
            if (Application.isEditor) return Path.Combine(Application.dataPath, "Scripts", "Input", "Face");
            string inBundle = Path.Combine(Application.dataPath, "GanryujimaFace");
            return Directory.Exists(inBundle) ? inBundle : Path.Combine(Path.GetDirectoryName(Application.dataPath), "GanryujimaFace");
        }

        private static string DefaultPage() => Application.isEditor ? Path.Combine(Path.GetDirectoryName(Application.dataPath), "public", "face-test.html") : Path.Combine(DefaultDirectory(), "face-test.html");

        private struct ReceivedSample { public FaceSample sample; public double time; }
        [Serializable] private sealed class EventTotals { public long lean, kiai, nod, shake; }
        [Serializable] private sealed class Status
        {
            public FaceState state;
            public EventTotals events;
            public bool ready, calibrating, previewRequested;
            public double remainingMs;
            public string error, calibrationStatus;
        }
    }
}