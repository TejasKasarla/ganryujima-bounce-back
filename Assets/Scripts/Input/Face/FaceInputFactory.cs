namespace Ganryujima.Input.Face
{
    public static class FaceInputFactory
    {
        // TODO(Dev A): Wire the local MediaPipe provider and DEBUG_INPUT fallback in milestone 2.
        public static IFaceInput createFaceInput() => new MockFaceInput();
    }
}
