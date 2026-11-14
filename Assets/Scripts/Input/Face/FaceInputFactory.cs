namespace Ganryujima.Input.Face
{
    public static class FaceInputFactory
    {
        // TODO(Dev A): Unity requires a C# provider; agree this port at merge.
        // Keep field names/ranges/methods; add real input + DEBUG_INPUT/failure fallback.
        // Bootstrap always returns the mock, even if DEBUG_INPUT is false.
        public static IFaceInput createFaceInput() => new MockFaceInput();
    }
}
