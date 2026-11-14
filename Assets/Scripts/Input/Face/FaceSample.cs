using System;

namespace Ganryujima.Input.Face
{
    [Serializable]
    public sealed class FaceSample
    {
        public bool tracking;
        public float headYaw, headPitch, headRoll, lateralOffset;
        public float jawOpen, browDownLeft, browDownRight;
        public float eyeSquintLeft, eyeSquintRight;
        public float mouthPucker, mouthFunnel, mouthSmileLeft, mouthSmileRight;
        public float eyeBlinkLeft, eyeBlinkRight;
        public float eyeLookInLeft, eyeLookOutLeft, eyeLookInRight, eyeLookOutRight;
        public float[] landmarks;

        public float Brow => (browDownLeft + browDownRight) * 0.5f;
        public float Squint => (eyeSquintLeft + eyeSquintRight) * 0.5f;
        public float Mouth => Math.Max(jawOpen, Math.Max(Math.Max(mouthPucker, mouthFunnel), Math.Max(mouthSmileLeft, mouthSmileRight)));
        public float HorizontalGaze => (eyeLookInLeft + eyeLookOutRight - eyeLookOutLeft - eyeLookInRight) * 0.5f;

        public bool IsValid()
        {
            if (!tracking) return false;
            for (int feature = 0; feature < 20; feature++)
            {
                float value = Feature(feature);
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                float limit = feature < 3 ? 180f : 1f;
                if (Math.Abs(value) > limit || (feature >= 4 && value < 0)) return false;
            }
            return true;
        }

        internal float Feature(int feature)
        {
            switch (feature)
            {
                case 0: return headYaw;
                case 1: return headPitch;
                case 2: return headRoll;
                case 3: return lateralOffset;
                case 4: return jawOpen;
                case 5: return browDownLeft;
                case 6: return browDownRight;
                case 7: return eyeSquintLeft;
                case 8: return eyeSquintRight;
                case 9: return mouthPucker;
                case 10: return mouthFunnel;
                case 11: return mouthSmileLeft;
                case 12: return mouthSmileRight;
                case 13: return eyeBlinkLeft;
                case 14: return eyeBlinkRight;
                case 15: return eyeLookInLeft;
                case 16: return eyeLookOutLeft;
                case 17: return eyeLookInRight;
                case 18: return eyeLookOutRight;
                default: return 0;
            }
        }
    }
}