using UnityEngine;

namespace Ganryujima.UI
{
    public static class GameFonts
    {
        public static Font Serif()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
#endif
        }
    }
}
