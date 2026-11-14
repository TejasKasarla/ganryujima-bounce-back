let FaceLandmarker, FilesetResolver, config, buildSample;

let landmarker, fileset, token, reader;
let running = false, previewRequested = false, previewBusy = false;
let delegate = 'GPU', lastInference = -Infinity, nextInference = 0, lastPreview = -Infinity;
let drawingCanvas, drawingContext, previewCanvas;

async function post(path, body, type = 'application/json') {
  const response = await fetch(path, {
    method: 'POST', headers: { 'X-Face-Token': token, 'Content-Type': type }, body,
    signal: AbortSignal.timeout(config.requestTimeoutMs),
  });
  if (!response.ok) throw new Error(`Face bridge ${response.status}`);
  await response.text();
}

async function loadDelegate(preferred) {
  landmarker?.close();
  delegate = preferred;
  landmarker = await FaceLandmarker.createFromOptions(fileset, {
    canvas: new OffscreenCanvas(config.webcamWidth, config.webcamHeight),
    baseOptions: { modelAssetPath: '/models/face_landmarker.task', delegate },
    runningMode: 'VIDEO', numFaces: 1,
    outputFaceBlendshapes: true, outputFacialTransformationMatrixes: true,
    minFaceDetectionConfidence: config.detectionConfidence,
    minFacePresenceConfidence: config.presenceConfidence,
    minTrackingConfidence: config.trackingConfidence,
  });
}

async function infer(image) {
  const now = performance.now();
  if (now < nextInference) return;
  const interval = 1000 / config.detectionHz;
  // Keep the clock phase rather than rounding every camera interval independently.
  nextInference = now + interval - ((now - nextInference) % interval);
  const previous = lastInference;
  lastInference = now;
  if (!drawingCanvas || drawingCanvas.width !== image.width || drawingCanvas.height !== image.height) {
    drawingCanvas = new OffscreenCanvas(image.width, image.height);
    drawingContext = drawingCanvas.getContext('2d');
  }
  drawingContext.drawImage(image, 0, 0);
  let result;
  try { result = landmarker.detectForVideo(drawingCanvas, now); }
  catch (error) {
    if (delegate !== 'GPU') throw error;
    await loadDelegate('CPU');
    result = landmarker.detectForVideo(drawingCanvas, performance.now());
  }
  const sample = buildSample(result, config);
  await post('/sample', JSON.stringify(sample));
  self.postMessage({ type: 'result', sample, delegate, hz: Number.isFinite(previous) ? 1000 / (now - previous) : 0 });
  if (previewRequested && !previewBusy && now - lastPreview >= 1000 / config.previewHz) {
    lastPreview = now;
    previewBusy = true;
    void sendPreview().finally(() => { previewBusy = false; });
  }
}

async function sendPreview() {
  try {
    const height = Math.round(config.previewWidth * drawingCanvas.height / drawingCanvas.width);
    if (!previewCanvas || previewCanvas.height !== height) previewCanvas = new OffscreenCanvas(config.previewWidth, height);
    previewCanvas.getContext('2d').drawImage(drawingCanvas, 0, 0, previewCanvas.width, height);
    const blob = await previewCanvas.convertToBlob({ type: 'image/jpeg', quality: config.previewQuality });
    await post('/preview', blob, 'image/jpeg');
  } catch { }
}

async function streamFrames(stream) {
  reader = stream.getReader();
  try {
    while (running) {
      const { value: frame, done } = await reader.read();
      if (done) break;
      try {
        if (performance.now() >= nextInference) {
          const bitmap = await createImageBitmap(frame);
          try { await infer(bitmap); } finally { bitmap.close(); }
        }
      } finally { frame.close(); }
    }
    if (running) throw new Error('Camera stream ended');
  } catch (error) { await fail(error); }
}

async function fail(error) {
  running = false;
  const message = error?.message ?? String(error);
  self.postMessage({ type: 'error', message });
  try { await post('/status', JSON.stringify({ ready: false, error: message })); } catch { }
}

self.onmessage = async ({ data }) => {
  try {
    if (data.type === 'init') {
      token = data.token;
      const [vision, settings, conversion] = await Promise.all([
        import('./vendor/vision_bundle.mjs'), import('./config.js'), import('./face-sample.mjs'),
      ]);
      ({ FaceLandmarker, FilesetResolver } = vision);
      config = settings.FACE_CONFIG;
      buildSample = conversion.buildSample;
      fileset = await FilesetResolver.forVisionTasks('/face/vendor/wasm');
      try { await loadDelegate('GPU'); } catch { await loadDelegate('CPU'); }
      running = true;
      self.postMessage({ type: 'ready', delegate });
    } else if (data.type === 'stream' && running) {
      void streamFrames(data.stream);
    } else if (data.type === 'frame') {
      try { if (running) await infer(data.bitmap); }
      finally { data.bitmap.close(); self.postMessage({ type: 'frame-done' }); }
    } else if (data.type === 'preview') previewRequested = data.enabled;
  } catch (error) { await fail(error); }
};