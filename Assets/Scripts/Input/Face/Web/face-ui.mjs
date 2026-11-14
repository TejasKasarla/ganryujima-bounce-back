import { FACE_CONFIG as config } from './config.js';
import { FaceTracker } from './face-tracker.mjs';

const byId = id => document.getElementById(id);
const token = document.querySelector('meta[name="face-token"]').content;
const tracker = new FaceTracker(byId('video'), token);
const fields = ['tracking', 'calibrated', 'leanX', 'leanTrigger', 'kiai', 'composure', 'nod', 'shake', 'wink', 'gaze', 'headYaw', 'headPitch', 'headRoll', 'timestamp'];
const outputs = new Map();
const flashes = new Map();
let previousCounts = {}, active = false, localError = '';

for (const name of fields) {
  const row = document.createElement('tr');
  const label = document.createElement('td');
  const value = document.createElement('td');
  label.textContent = name;
  value.textContent = '--';
  value.id = `value-${name}`;
  row.append(label, value);
  byId('fields').append(row);
  outputs.set(name, value);
}

function showError(message) { byId('error').textContent = message || ''; byId('error').hidden = !message; }
function buttons(running, starting = false) {
  active = running;
  byId('start').disabled = running || starting;
  byId('stop').disabled = !running && !starting;
  byId('calibrate').disabled = !running;
}

byId('start').addEventListener('click', async () => {
  localError = '';
  showError('');
  previousCounts = {};
  buttons(false, true);
  const success = await tracker.start();
  buttons(success);
});
byId('stop').addEventListener('click', () => { tracker.stop(); buttons(false); clearLandmarks(); });
byId('calibrate').addEventListener('click', async () => {
  try { await tracker.post('/calibrate', {}); } catch (error) { showError(error.message); }
});
tracker.addEventListener('status', event => { byId('status').textContent = event.detail; });
tracker.addEventListener('delegate', event => { byId('delegate').textContent = event.detail; });
tracker.addEventListener('error', event => { localError = event.detail; showError(localError); buttons(false); clearLandmarks(); });
tracker.addEventListener('sample', ({ detail }) => {
  byId('delegate').textContent = detail.delegate;
  byId('fps').textContent = `${detail.hz.toFixed(1)} Hz`;
  byId('camera-state').textContent = detail.sample.tracking ? 'FACE DETECTED' : 'SEARCHING';
  drawLandmarks(detail.sample.landmarks);
});

function clearLandmarks() {
  const canvas = byId('landmarks');
  canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
  byId('camera-state').textContent = 'OFFLINE';
}

function drawLandmarks(points) {
  const canvas = byId('landmarks');
  const video = byId('video');
  if (video.videoWidth && (canvas.width !== video.videoWidth || canvas.height !== video.videoHeight)) {
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
  }
  const context = canvas.getContext('2d');
  context.clearRect(0, 0, canvas.width, canvas.height);
  context.fillStyle = '#7ce9bb';
  if (!points) return;
  context.beginPath();
  for (let point = 0; point < points.length; point += 3) {
    const horizontal = points[point] * canvas.width;
    const vertical = points[point + 1] * canvas.height;
    context.moveTo(horizontal + 1.1, vertical);
    context.arc(horizontal, vertical, 1.1, 0, Math.PI * 2);
  }
  context.fill();
}

function display(name, value) {
  if (name === 'gaze' && typeof value === 'number') return ['left', 'center', 'right', 'unknown'][value] ?? 'unknown';
  if (name === 'wink' && typeof value === 'number') return ['none', 'left', 'right'][value] ?? 'none';
  return typeof value === 'number' ? value.toFixed(name === 'timestamp' || name === 'leanTrigger' ? 0 : 3) : String(value ?? '--');
}

async function poll() {
  try {
    const response = await fetch('/state', { headers: { 'X-Face-Token': token }, signal: AbortSignal.timeout(config.requestTimeoutMs) });
    if (!response.ok) throw new Error(`Bridge ${response.status}`);
    const status = await response.json();
    byId('connection').textContent = 'Bridge connected';
    for (const name of fields) outputs.get(name).textContent = display(name, status.state?.[name]);
    byId('calibration-status').textContent = status.calibrationStatus ?? 'Not calibrated';
    byId('countdown').hidden = !status.calibrating;
    byId('countdown').textContent = Math.ceil((status.remainingMs ?? 0) / 1000);
    byId('calibrate').disabled = !active || status.calibrating;
    showError(localError || status.error);
    tracker.setPreview(Boolean(status.previewRequested));
    for (const name of ['lean', 'kiai', 'nod', 'shake']) {
      const count = status.events?.[name] ?? 0;
      if (count > (previousCounts[name] ?? 0)) flashes.set(name, performance.now() + config.eventFlashMs);
      previousCounts[name] = count;
      byId(`event-${name}`).classList.toggle('fired', (flashes.get(name) ?? 0) > performance.now());
    }
  } catch (error) { byId('connection').textContent = 'Bridge disconnected'; showError(error.message); }
  setTimeout(poll, 1000 / config.detectionHz);
}
void poll();
addEventListener('pagehide', () => tracker.stop());