import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import { buildSample } from '../Web/face-sample.mjs';

const config = { yawSign: 1, pitchSign: 1, rollSign: -1 };
assert.deepEqual(buildSample({}, config), { tracking: false });
const matrix = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1];
const result = {
  faceLandmarks: [Array.from({ length: 478 }, () => ({ x: 0.6, y: 0.5, z: 0 }))],
  facialTransformationMatrixes: [{ data: matrix }],
  faceBlendshapes: [{ categories: [{ categoryName: 'jawOpen', score: 0.7 }] }],
};
const sample = buildSample(result, config);
assert.equal(sample.tracking, true);
assert.equal(sample.jawOpen, 0.7);
assert.equal(sample.eyeBlinkLeft, 0);
assert.ok(Math.abs(sample.headYaw) < 0.001 && Math.abs(sample.headPitch) < 0.001 && Math.abs(sample.headRoll) < 0.001);
assert.ok(Math.abs(sample.lateralOffset + 0.1) < 0.001);
assert.equal(sample.landmarks.length, 1434);
const angle = Math.PI / 6;
matrix[0] = Math.cos(angle);
matrix[1] = Math.sin(angle);
matrix[4] = -Math.sin(angle);
matrix[5] = Math.cos(angle);
assert.ok(Math.abs(buildSample(result, config).headRoll + 30) < 0.001);
matrix[0] = NaN;
assert.equal(buildSample(result, config).tracking, false);
const page = await readFile(new URL('../../../../../public/face-test.html', import.meta.url), 'utf8');
assert.ok(!page.includes('/Core/') && !page.includes('GameLoop'));
for (const name of ['video', 'landmarks', 'calibrate', 'countdown', 'fields']) assert.ok(page.includes(`id="${name}"`));
console.log('PASS: browser sample conversion and standalone-page checks');
const thresholds = await readFile(new URL('../FaceConfig.cs', import.meta.url), 'utf8');
const guide = await readFile(new URL('../README.md', import.meta.url), 'utf8');
const constants = [...thresholds.matchAll(/public const \w+ (\w+)\s*=/g)].map(match => match[1]);
for (const name of constants) assert.ok(guide.includes(`\`${name}\``), `Missing tuning documentation: ${name}`);
console.log(`PASS: all ${constants.length} FaceConfig settings appear in the tuning guide`);
const seenGuids = new Set();
for (const name of (await readdir(new URL('../', import.meta.url))).filter(name => name.endsWith('.cs'))) {
  const metadata = await readFile(new URL(`../${name}.meta`, import.meta.url), 'utf8');
  const guid = metadata.match(/^guid: ([0-9a-f]{32})$/m)?.[1];
  assert.ok(guid && !seenGuids.has(guid), `Missing or duplicate Unity script GUID: ${name}`);
  seenGuids.add(guid);
}
console.log(`PASS: ${seenGuids.size} Unity face script GUIDs are present and unique`);