const blendshapeNames = [
  'jawOpen', 'browDownLeft', 'browDownRight', 'eyeSquintLeft', 'eyeSquintRight',
  'mouthPucker', 'mouthFunnel', 'mouthSmileLeft', 'mouthSmileRight',
  'eyeBlinkLeft', 'eyeBlinkRight', 'eyeLookInLeft', 'eyeLookOutLeft', 'eyeLookInRight', 'eyeLookOutRight',
];

export function buildSample(result, config) {
  const points = result.faceLandmarks?.[0];
  const matrix = result.facialTransformationMatrixes?.[0]?.data;
  const shapes = result.faceBlendshapes?.[0]?.categories;
  if (!points?.length || matrix?.length !== 16 || !shapes?.length || !Array.from(matrix).every(Number.isFinite)) {
    return { tracking: false };
  }
  const degrees = 180 / Math.PI;
  const sample = {
    tracking: true,
    headYaw: Math.atan2(-matrix[2], Math.hypot(matrix[0], matrix[1])) * degrees * config.yawSign,
    headPitch: Math.atan2(matrix[6], matrix[10]) * degrees * config.pitchSign,
    headRoll: Math.atan2(matrix[1], matrix[0]) * degrees * config.rollSign,
    lateralOffset: 0.5 - ((points[234]?.x ?? points[0].x) + (points[454]?.x ?? points[0].x)) / 2,
    landmarks: points.flatMap(point => [point.x, point.y, point.z]),
  };
  const scores = new Map(shapes.map(shape => [shape.categoryName, shape.score]));
  for (const name of blendshapeNames) sample[name] = Math.max(0, Math.min(1, scores.get(name) ?? 0));
  return sample;
}