using UnityEditor;
using UnityEngine;

namespace Ganryujima.Editor
{
    public static class ContentSetup
    {
        [MenuItem("Ganryujima/Prepare content textures")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            string path="Assets/Resources/Art/Coast/SandNormal.jpg";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.NormalMap; importer.sRGBTexture=false; importer.maxTextureSize=1024; importer.SaveAndReimport();
            Debug.Log("CONTENT TEXTURES READY");
        }
        public static void PrepareAndSmoke() { Prepare(); ProjectSetup.Smoke(); }
    }
}
