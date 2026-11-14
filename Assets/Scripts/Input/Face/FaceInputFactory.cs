using Ganryujima.Core;

namespace Ganryujima.Input.Face
{
    public static class FaceInputFactory
    {
        public static IFaceInput Active { get; private set; }
        public static bool CameraOverride { get; set; }

        public static IFaceInput createFaceInput() => createFaceInput(GameConfig.DEBUG_INPUT && !CameraOverride);

        public static IFaceInput createFaceInput(bool debugInput)
        {
            Active = debugInput ? (IFaceInput)new MockFaceInput() : new FallbackFaceInput();
            return Active;
        }

        public static FaceInput CameraOf(IFaceInput provider) => provider is FallbackFaceInput fallback ? fallback.Camera : provider as FaceInput;
    }
}
