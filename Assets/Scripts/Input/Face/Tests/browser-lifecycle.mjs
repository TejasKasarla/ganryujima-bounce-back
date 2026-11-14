import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const workers = [], readiness = [], streams = [];
let holdReadiness = false;
let denyPermission = false;
let playGate;
const context = vm.createContext({
  EventTarget, CustomEvent, DOMException, URL, AbortSignal, setTimeout, clearTimeout,
  navigator: {
    mediaDevices: {
      async getUserMedia() {
        if (denyPermission) throw new DOMException('Denied by test', 'NotAllowedError');
        const track = { id: streams.length, stopped: false, stop() { this.stopped = true; }, addEventListener() {} };
        const stream = { getTracks: () => [track], getVideoTracks: () => [track] };
        streams.push(stream);
        return stream;
      },
    },
  },
  Worker: class {
    constructor() { this.messages = []; workers.push(this); }
    postMessage(message) {
      this.messages.push(message);
      if (message.type === 'init') queueMicrotask(() => this.onmessage?.({ data: { type: 'ready', delegate: 'GPU' } }));
    }
    terminate() { this.terminated = true; }
  },
  MediaStreamTrackProcessor: class { constructor({ track }) { this.readable = { track }; } },
  async fetch(path, options) {
    const body = JSON.parse(options.body);
    if (path === '/status' && body.ready && holdReadiness) {
      await new Promise(resolve => readiness.push(resolve));
    }
    return { ok: true, text: async () => '{}' };
  },
});

const module = new vm.SourceTextModule(await readFile(new URL('../Web/face-tracker.mjs', import.meta.url), 'utf8'), {
  context,
  initializeImportMeta(meta) { meta.url = 'http://127.0.0.1/face/face-tracker.mjs'; },
});
await module.link(async specifier => {
  assert.equal(specifier, './config.js');
  return new vm.SyntheticModule(['FACE_CONFIG'], function () {
    this.setExport('FACE_CONFIG', { webcamWidth: 640, webcamHeight: 480, requestTimeoutMs: 5000, modelLoadTimeoutMs: 45000, detectionHz: 30 });
  }, { context });
});
await module.evaluate();
const Tracker = module.namespace.FaceTracker;
const video = { srcObject: null, async play() { if (playGate) await playGate; } };
const tracker = new Tracker(video, 'test-only-token');

denyPermission = true;
assert.equal(await tracker.start(), false);
assert.equal(tracker.running, false);
assert.equal(tracker.starting, false);
denyPermission = false;

holdReadiness = true;
const first = tracker.start();
await until(() => readiness.length === 1);
tracker.stop(false);
assert.equal(streams[0].getTracks()[0].stopped, true);
const second = tracker.start();
await until(() => readiness.length === 2);
readiness[0]();
assert.equal(await first, false);
assert.equal(tracker.starting, true, 'stale startup must not reset newer starting flag');
workers[0].onmessage({ data: { type: 'error', message: 'Stale worker failure' } });
workers[0].onerror({ message: 'Stale worker exception' });
assert.equal(tracker.starting, true, 'stale worker errors must not stop the replacement session');
assert.equal(workers[1].messages.filter(message => message.type === 'stream').length, 0);
readiness[1]();
assert.equal(await second, true);
const transfers = workers[1].messages.filter(message => message.type === 'stream');
assert.equal(transfers.length, 1);
assert.equal(transfers[0].stream.track, streams[1].getTracks()[0]);
tracker.stop(false);
assert.equal(workers[1].terminated, true);
assert.equal(streams[1].getTracks()[0].stopped, true);

holdReadiness = false;
let releasePlay;
playGate = new Promise(resolve => { releasePlay = resolve; });
const third = tracker.start();
await until(() => video.srcObject !== null);
tracker.stop(false);
releasePlay();
assert.equal(await third, false);
assert.equal(workers.length, 2, 'stopped video startup must not create another worker');
console.log('PASS: camera denial, stop/restart race, stale readiness, and camera/worker cleanup');

async function until(predicate) {
  for (let attempt = 0; attempt < 100; attempt++) {
    if (predicate()) return;
    await Promise.resolve();
  }
  throw new Error('Expected asynchronous test stage was not reached');
}