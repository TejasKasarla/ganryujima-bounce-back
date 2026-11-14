import { FACE_CONFIG as config } from './config.js';

export class FaceTracker extends EventTarget {
  constructor(video, token) {
    super();
    this.video = video;
    this.token = token;
    this.generation = 0;
    this.running = false;
    this.busy = false;
  }

  emit(type, detail) { this.dispatchEvent(new CustomEvent(type, { detail })); }

  async post(path, value) {
    const response = await fetch(path, {
      method: 'POST', headers: { 'X-Face-Token': this.token, 'Content-Type': 'application/json' },
      body: JSON.stringify(value), signal: AbortSignal.timeout(config.requestTimeoutMs),
    });
    if (!response.ok) throw new Error(`Face bridge ${response.status}`);
    await response.text();
  }

  async start() {
    if (this.running || this.starting) return false;
    this.starting = true;
    const generation = ++this.generation;
    try {
      this.emit('status', 'Requesting camera');
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { width: { ideal: config.webcamWidth }, height: { ideal: config.webcamHeight }, facingMode: 'user' }, audio: false,
      });
      if (generation !== this.generation) { stream.getTracks().forEach(track => track.stop()); return false; }
      this.stream = stream;
      this.video.srcObject = stream;
      await this.video.play();
      if (generation !== this.generation) return false;
      this.emit('status', 'Loading local model');
      this.worker = new Worker(new URL('./face-tracker.worker.mjs', import.meta.url));
      await new Promise((resolve, reject) => {
        const timeout = setTimeout(() => reject(new Error('Local model load timed out')), config.modelLoadTimeoutMs);
        this.cancelInitialization = () => { clearTimeout(timeout); reject(new DOMException('Camera stopped', 'AbortError')); };
        this.worker.onerror = event => {
          if (generation !== this.generation) return;
          clearTimeout(timeout);
          reject(new Error(event.message || 'Face worker failed'));
          this.fail(event.message || 'Face worker failed');
        };
        this.worker.onmessage = ({ data }) => {
          if (generation !== this.generation) return;
          if (data.type === 'ready') { clearTimeout(timeout); this.cancelInitialization = null; resolve(); this.emit('delegate', data.delegate); }
          else if (data.type === 'result') this.emit('sample', data);
          else if (data.type === 'frame-done') this.busy = false;
          else if (data.type === 'error') { clearTimeout(timeout); reject(new Error(data.message)); this.fail(data.message); }
        };
        this.worker.postMessage({ type: 'init', token: this.token });
      });
      if (generation !== this.generation) return false;
      this.running = true;
      await this.post('/status', { ready: true });
      if (generation !== this.generation) return false;
      const track = stream.getVideoTracks()[0];
      track.addEventListener('ended', () => { if (this.running) this.fail('Camera disconnected'); }, { once: true });
      if ('MediaStreamTrackProcessor' in globalThis) {
        const processor = new MediaStreamTrackProcessor({ track });
        this.worker.postMessage({ type: 'stream', stream: processor.readable }, [processor.readable]);
      } else this.captureFrame(generation);
      this.emit('status', 'Tracking');
      return true;
    } catch (error) {
      if (generation === this.generation) await this.fail(error.message || String(error));
      return false;
    } finally { if (generation === this.generation) this.starting = false; }
  }

  captureFrame(generation) {
    if (!this.running || generation !== this.generation) return;
    this.timer = setTimeout(() => this.captureFrame(generation), Math.ceil(1000 / config.detectionHz));
    if (this.busy || this.video.readyState < 2) return;
    this.busy = true;
    createImageBitmap(this.video).then(bitmap => {
      if (!this.running || generation !== this.generation) { bitmap.close(); this.busy = false; return; }
      this.worker.postMessage({ type: 'frame', bitmap }, [bitmap]);
    }).catch(error => { this.busy = false; this.fail(error.message); });
  }

  setPreview(enabled) { this.worker?.postMessage({ type: 'preview', enabled }); }

  async fail(message) {
    this.stop(false);
    this.emit('error', message);
    try { await this.post('/status', { ready: false, error: message }); } catch { }
  }

  stop(notify = true) {
    this.generation++;
    this.running = false;
    this.starting = false;
    this.cancelInitialization?.();
    this.cancelInitialization = null;
    clearTimeout(this.timer);
    this.worker?.terminate();
    this.worker = null;
    this.stream?.getTracks().forEach(track => track.stop());
    this.stream = null;
    this.video.srcObject = null;
    this.busy = false;
    this.emit('status', 'Camera off');
    if (notify) void this.post('/status', { ready: false }).catch(() => {});
  }
}