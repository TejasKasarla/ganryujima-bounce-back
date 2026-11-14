using System;

namespace Ganryujima.Input.Face
{
    /// <summary>Camera-independent signal processing; all times use one monotonic millisecond clock.</summary>
    public sealed class FaceSignalProcessor
    {
        private FaceState state = new FaceState { composure = 1, gaze = Gaze.unknown };
        private readonly double[] means = new double[19];
        private readonly double[] moments = new double[19];
        private readonly float[] neutral = new float[19];
        private readonly Oscillation nod = new Oscillation();
        private readonly Oscillation shake = new Oscillation();
        private double lastSeen = double.NegativeInfinity, lastTick, frameTime;
        private double calibrationStart, calibrationEnd, calibrationCoverage, calibrationLastSample;
        private int calibrationSamples;
        private float jawThreshold = FaceConfig.JawOpenThreshold;
        private float leftBlinkThreshold = FaceConfig.BlinkClosedThreshold;
        private float rightBlinkThreshold = FaceConfig.BlinkClosedThreshold;
        private float headSpeedRange = FaceConfig.HeadSpeedRange, headSpeed, lastMouth;
        private float previousYaw, previousPitch, previousRoll;
        private double jawSince = double.NaN, winkSince = double.NaN, gazeSince;
        private double lastKiai = double.NegativeInfinity, lastLean = double.NegativeInfinity;
        private double lastGesture = double.NegativeInfinity;
        private bool jawLatched, leanArmed = true;
        private Wink winkCandidate;
        private Gaze gazeCandidate = Gaze.unknown;

        public FaceState State => state;
        public bool IsCalibrating { get; private set; }
        public string CalibrationStatus { get; private set; } = "Not calibrated";
        public double CalibrationRemainingMs => IsCalibrating ? Math.Max(0, calibrationEnd - frameTime) : 0;
        public float JawThreshold => jawThreshold;

        public void BeginCalibration(double now, int durationMs = FaceConfig.CalibrationDurationMs)
        {
            IsCalibrating = true;
            state.calibrated = false;
            calibrationStart = frameTime = now;
            calibrationEnd = now + Math.Max(1, durationMs);
            calibrationLastSample = double.NaN;
            calibrationSamples = 0;
            calibrationCoverage = 0;
            Array.Clear(means, 0, means.Length);
            Array.Clear(moments, 0, moments.Length);
            ResetGestures();
            CalibrationStatus = "Calibrating";
        }

        public void CancelCalibration()
        {
            if (IsCalibrating) CalibrationStatus = "Calibration cancelled";
            IsCalibrating = false;
        }

        public void BeginFrame(double now)
        {
            frameTime = now;
            state.timestamp = now;
            state.leanTrigger = 0;
            state.kiai = state.nod = state.shake = false;
        }

        public void Process(FaceSample sample, double receivedAt)
        {
            if (sample == null || !sample.IsValid())
            {
                ResetGestures();
                calibrationLastSample = double.NaN;
                return;
            }
            if (receivedAt < lastSeen || frameTime - receivedAt > FaceConfig.TrackingHoldMs) return;
            bool reacquired = !state.tracking || receivedAt - lastSeen > FaceConfig.TrackingHoldMs;
            double elapsed = reacquired ? 1000.0 / FaceConfig.DetectionHz : Math.Max(1, receivedAt - lastSeen);
            if (reacquired) ResetGestures();
            lastSeen = receivedAt;
            state.tracking = true;

            if (IsCalibrating && receivedAt >= calibrationStart && receivedAt <= calibrationEnd) AccumulateCalibration(sample, receivedAt);
            float yaw = Difference(sample.headYaw, neutral[0]);
            float pitch = Difference(sample.headPitch, neutral[1]);
            float roll = Difference(sample.headRoll, neutral[2]);
            state.headYaw = SmoothAngle(state.headYaw, yaw, elapsed);
            state.headPitch = SmoothAngle(state.headPitch, pitch, elapsed);
            state.headRoll = SmoothAngle(state.headRoll, roll, elapsed);
            float lateral = sample.lateralOffset - neutral[3];
            float lean = Clamp(roll / FaceConfig.LeanRollDegrees * FaceConfig.LeanRollWeight +
                yaw / FaceConfig.LeanYawDegrees * FaceConfig.LeanYawWeight +
                lateral / FaceConfig.LeanLateralDistance * FaceConfig.LeanLateralWeight, -1, 1);
            state.leanX = Smooth(state.leanX, lean, elapsed, FaceConfig.PoseSmoothingMs);

            float speed = reacquired ? 0 : (float)(Math.Sqrt(Square(Difference(yaw, previousYaw)) +
                Square(Difference(pitch, previousPitch)) + Square(Difference(roll, previousRoll))) * 1000 / elapsed);
            headSpeed = Smooth(headSpeed, speed, elapsed, FaceConfig.HeadSpeedSmoothingMs);
            previousYaw = yaw;
            previousPitch = pitch;
            previousRoll = roll;
            float restingMouth = Math.Max(neutral[4], Math.Max(Math.Max(neutral[9], neutral[10]), Math.Max(neutral[11], neutral[12])));
            float mouthMovement = reacquired ? 0 : (float)(Math.Abs(sample.Mouth - lastMouth) * 1000 / elapsed);
            lastMouth = sample.Mouth;
            float tension = Unit((sample.Brow - (neutral[5] + neutral[6]) * 0.5f) / FaceConfig.BrowRange) * FaceConfig.BrowWeight +
                Unit((sample.Squint - (neutral[7] + neutral[8]) * 0.5f) / FaceConfig.SquintRange) * FaceConfig.SquintWeight +
                Math.Max(Unit((sample.Mouth - restingMouth) / FaceConfig.MouthRange), Unit(mouthMovement / FaceConfig.MouthSpeedRange)) * FaceConfig.MouthWeight +
                Unit(headSpeed / headSpeedRange) * FaceConfig.HeadStabilityWeight;
            state.composure = Smooth(state.composure, Unit(1 - tension), elapsed, FaceConfig.ComposureSmoothingMs);

            if (!state.calibrated || IsCalibrating)
            {
                state.wink = Wink.none;
                state.gaze = Gaze.unknown;
                return;
            }
            UpdateLean(receivedAt);
            UpdateJaw(sample.jawOpen, receivedAt);
            UpdateEyes(sample, receivedAt);
            if (receivedAt - lastGesture >= FaceConfig.GestureCooldownMs)
            {
                if (nod.Update(state.headPitch, state.headYaw, FaceConfig.NodAmplitudeDegrees, receivedAt))
                {
                    state.nod = true;
                    lastGesture = receivedAt;
                    shake.Reset();
                }
                else if (shake.Update(state.headYaw, state.headPitch, FaceConfig.ShakeAmplitudeDegrees, receivedAt))
                {
                    state.shake = true;
                    lastGesture = receivedAt;
                    nod.Reset();
                }
            }
        }

        public void EndFrame(double now)
        {
            if (IsCalibrating && now >= calibrationEnd) FinishCalibration();
            if (now - lastSeen > FaceConfig.TrackingHoldMs)
            {
                state.tracking = false;
                double elapsed = Math.Max(0, now - Math.Max(lastTick, lastSeen + FaceConfig.TrackingHoldMs));
                float decay = (float)Math.Exp(-elapsed / FaceConfig.TrackingDecayMs);
                state.leanX *= decay;
                state.headYaw *= decay;
                state.headPitch *= decay;
                state.headRoll *= decay;
                state.composure = 1 - (1 - state.composure) * decay;
                state.wink = Wink.none;
                state.gaze = Gaze.unknown;
                ResetGestures();
            }
            lastTick = now;
        }

        private void UpdateLean(double now)
        {
            if (Math.Abs(state.leanX) < FaceConfig.LeanReleaseThreshold) leanArmed = true;
            if (leanArmed && Math.Abs(state.leanX) >= FaceConfig.LeanTriggerThreshold && now - lastLean >= FaceConfig.LeanCooldownMs)
            {
                if (state.leanTrigger == 0) state.leanTrigger = Math.Sign(state.leanX);
                leanArmed = false;
                lastLean = now;
            }
        }

        private void UpdateJaw(float jaw, double now)
        {
            float release = neutral[4] + (jawThreshold - neutral[4]) * FaceConfig.JawReleaseRatio;
            if (jaw < release) { jawSince = double.NaN; jawLatched = false; }
            if (jaw < jawThreshold) { jawSince = double.NaN; return; }
            if (double.IsNaN(jawSince)) jawSince = now;
            if (!jawLatched && now - jawSince >= FaceConfig.KiaiHoldMs && now - lastKiai >= FaceConfig.KiaiCooldownMs)
            {
                state.kiai = true;
                jawLatched = true;
                lastKiai = now;
            }
        }

        private void UpdateEyes(FaceSample sample, double now)
        {
            Wink candidate = sample.eyeBlinkLeft >= leftBlinkThreshold && sample.eyeBlinkRight <= Math.Max(FaceConfig.BlinkOtherEyeMaximum, neutral[14]) ? Wink.left :
                sample.eyeBlinkRight >= rightBlinkThreshold && sample.eyeBlinkLeft <= Math.Max(FaceConfig.BlinkOtherEyeMaximum, neutral[13]) ? Wink.right : Wink.none;
            if (candidate != winkCandidate) { winkCandidate = candidate; winkSince = now; }
            state.wink = candidate != Wink.none && now - winkSince >= FaceConfig.WinkHoldMs ? candidate : Wink.none;

            if (sample.eyeBlinkLeft >= leftBlinkThreshold || sample.eyeBlinkRight >= rightBlinkThreshold)
            {
                state.gaze = gazeCandidate = Gaze.unknown;
                return;
            }
            float restingGaze = (neutral[15] + neutral[18] - neutral[16] - neutral[17]) * 0.5f;
            float offset = sample.HorizontalGaze - restingGaze;
            float threshold = state.gaze == Gaze.center || state.gaze == Gaze.unknown ? FaceConfig.GazeThreshold : FaceConfig.GazeReleaseThreshold;
            Gaze target = offset < -threshold ? Gaze.left : offset > threshold ? Gaze.right : Gaze.center;
            if (target != gazeCandidate) { gazeCandidate = target; gazeSince = now; }
            if (now - gazeSince >= FaceConfig.GazeHoldMs) state.gaze = target;
        }

        private void AccumulateCalibration(FaceSample sample, double now)
        {
            calibrationSamples++;
            if (!double.IsNaN(calibrationLastSample)) calibrationCoverage += Math.Min(FaceConfig.CalibrationMaximumSampleGapMs, Math.Max(0, now - calibrationLastSample));
            calibrationLastSample = now;
            for (int feature = 0; feature < means.Length; feature++)
            {
                double value = sample.Feature(feature);
                double delta = value - means[feature];
                means[feature] += delta / calibrationSamples;
                moments[feature] += delta * (value - means[feature]);
            }
        }

        private void FinishCalibration()
        {
            IsCalibrating = false;
            if (calibrationSamples < FaceConfig.CalibrationMinimumSamples || calibrationCoverage < (calibrationEnd - calibrationStart) * FaceConfig.CalibrationCoverage)
            {
                CalibrationStatus = "Calibration failed: insufficient face tracking; retry";
                return;
            }
            for (int feature = 0; feature < neutral.Length; feature++) neutral[feature] = (float)means[feature];
            jawThreshold = Clamp(neutral[4] + Math.Max(FaceConfig.CalibrationJawMargin, Noise(4)), FaceConfig.JawOpenThreshold, FaceConfig.MaximumJawThreshold);
            leftBlinkThreshold = Clamp(neutral[13] + Math.Max(FaceConfig.CalibrationBlinkMargin, Noise(13)), FaceConfig.BlinkClosedThreshold, FaceConfig.MaximumBlinkThreshold);
            rightBlinkThreshold = Clamp(neutral[14] + Math.Max(FaceConfig.CalibrationBlinkMargin, Noise(14)), FaceConfig.BlinkClosedThreshold, FaceConfig.MaximumBlinkThreshold);
            headSpeedRange = Math.Max(FaceConfig.HeadSpeedRange, Math.Max(Noise(0), Math.Max(Noise(1), Noise(2))) * FaceConfig.DetectionHz);
            state.calibrated = true;
            state.headYaw = state.headPitch = state.headRoll = state.leanX = 0;
            state.composure = 1;
            previousYaw = previousPitch = previousRoll = headSpeed = 0;
            lastMouth = Math.Max(neutral[4], Math.Max(Math.Max(neutral[9], neutral[10]), Math.Max(neutral[11], neutral[12])));
            ResetGestures();
            CalibrationStatus = "Calibrated";
        }

        private float Noise(int feature) => (float)(Math.Sqrt(moments[feature] / Math.Max(1, calibrationSamples - 1)) * FaceConfig.CalibrationNoiseMultiplier);

        private void ResetGestures()
        {
            jawSince = winkSince = double.NaN;
            winkCandidate = Wink.none;
            jawLatched = false;
            leanArmed = Math.Abs(state.leanX) < FaceConfig.LeanReleaseThreshold;
            gazeCandidate = Gaze.unknown;
            nod.Reset();
            shake.Reset();
        }

        private static float Smooth(float current, float target, double elapsed, int smoothingMs) => current + (target - current) * (float)(1 - Math.Exp(-elapsed / smoothingMs));
        private static float SmoothAngle(float current, float target, double elapsed) => current + Difference(target, current) * (float)(1 - Math.Exp(-elapsed / FaceConfig.PoseSmoothingMs));
        private static float Difference(float angle, float origin) => ((angle - origin + 540) % 360) - 180;
        private static double Square(float value) => (double)value * value;
        private static float Unit(float value) => Clamp(value, 0, 1);
        private static float Clamp(float value, float minimum, float maximum) => Math.Max(minimum, Math.Min(maximum, value));

        private sealed class Oscillation
        {
            private int direction;
            private bool reversed;
            private double began;

            public bool Update(float axis, float other, float amplitude, double now)
            {
                if (direction != 0 && now - began > FaceConfig.GestureWindowMs) Reset();
                if (Math.Abs(other) > Math.Max(amplitude, Math.Abs(axis)) * FaceConfig.GestureAxisDominance) { Reset(); return false; }
                if (direction == 0 && Math.Abs(axis) >= amplitude) { direction = Math.Sign(axis); began = now; }
                else if (direction != 0 && Math.Abs(axis) >= amplitude && Math.Sign(axis) != direction) reversed = true;
                else if (reversed && Math.Abs(axis) <= amplitude * FaceConfig.GestureReleaseRatio) { Reset(); return true; }
                return false;
            }

            public void Reset() { direction = 0; reversed = false; }
        }
    }
}