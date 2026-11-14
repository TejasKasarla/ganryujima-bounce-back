namespace Ganryujima.Input.Face
{
    public static class FaceConfig
    {
        public const int DetectionHz = 30;
        public const int TrackingHoldMs = 300;
        public const int TrackingDecayMs = 450;
        public const int PoseSmoothingMs = 90;
        public const int ComposureSmoothingMs = 250;
        public const int CalibrationDurationMs = 5000;
        public const int CalibrationMinimumSamples = 45;
        public const float CalibrationCoverage = 0.5f;
        public const float CalibrationNoiseMultiplier = 3f;
        public const float MinimumDetectionConfidence = 0.5f;
        public const float MinimumPresenceConfidence = 0.5f;
        public const float MinimumTrackingConfidence = 0.5f;

        public const float LeanRollDegrees = 18f;
        public const float LeanYawDegrees = 28f;
        public const float LeanLateralDistance = 0.16f;
        public const float LeanRollWeight = 0.45f;
        public const float LeanYawWeight = 0.2f;
        public const float LeanLateralWeight = 0.35f;
        public const float LeanTriggerThreshold = 0.5f;
        public const float LeanReleaseThreshold = 0.25f;
        public const int LeanCooldownMs = 500;

        public const float JawOpenThreshold = 0.38f;
        public const float JawReleaseRatio = 0.55f;
        public const int KiaiHoldMs = 120;
        public const int KiaiCooldownMs = 750;
        public const float BrowRange = 0.5f;
        public const float SquintRange = 0.6f;
        public const float MouthRange = 0.45f;
        public const float HeadSpeedRange = 100f;
        public const float BrowWeight = 0.25f;
        public const float SquintWeight = 0.2f;
        public const float MouthWeight = 0.3f;
        public const float HeadStabilityWeight = 0.25f;

        public const float NodAmplitudeDegrees = 9f;
        public const float ShakeAmplitudeDegrees = 13f;
        public const float GestureReleaseRatio = 0.45f;
        public const float GestureAxisDominance = 1.15f;
        public const int GestureWindowMs = 1200;
        public const int GestureCooldownMs = 900;
        public const float BlinkClosedThreshold = 0.65f;
        public const float BlinkOtherEyeMaximum = 0.3f;
        public const int WinkHoldMs = 250;
        public const float GazeThreshold = 0.18f;
        public const float GazeReleaseThreshold = 0.1f;
        public const int GazeHoldMs = 100;

        public const float MockCalmComposure = 0.95f;
        public const float MockLowComposure = 0.15f;
        public const int BridgePort = 8765;
        public const int MaximumRequestBytes = 262144;
        public const int MaximumPendingSamples = 8;
        public const int BrowserHeartbeatTimeoutMs = 3000;
        public const int PreviewHz = 8;
        public const int PreviewWidth = 320;
        public const float PreviewJpegQuality = 0.65f;
    }
}