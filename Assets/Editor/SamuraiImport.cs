using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ganryujima.Editor
{
    public static class SamuraiImport
    {
        public const string Source = "Assets/Art/Samurai/Samurai.fbx";
        [MenuItem("Ganryujima/Prepare samurai character")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Source);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.isReadable = false;
            importer.SaveAndReimport();
            string textures = "Assets/Art/Samurai/Textures";
            Directory.CreateDirectory(textures);
            importer.ExtractTextures(textures);
            AssetDatabase.Refresh();
            Texture2D Map(string suffix, bool linear = false, bool normal = false)
            {
                string path = Directory.GetFiles(textures, "*"+suffix+".jpg").Single();
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=!linear && !normal;
                ti.isReadable=linear && !normal;
                ti.maxTextureSize=2048;
                ti.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var color=Map("basecolor"); var normalMap=Map("normal",true,true);
            var metal=Map("metallic",true); var rough=Map("roughness",true);
            var packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);
            var pixels=metal.GetPixels(); var roughness=rough.GetPixels();
            for(int i=0;i<pixels.Length;i++) pixels[i]=new Color(pixels[i].r,0,0,1-roughness[i].r);
            packed.SetPixels(pixels); packed.Apply();
            string packedPath=textures+"/ArmorMetallicSmoothness.png";
            File.WriteAllBytes(packedPath,packed.EncodeToPNG()); Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(packedPath);
            var pi=(TextureImporter)AssetImporter.GetAtPath(packedPath); pi.sRGBTexture=false; pi.SaveAndReimport();
            string matPath="Assets/Art/Samurai/Armor.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null) { material=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material,matPath); }
            material.mainTexture=color; material.color=Color.white;
            material.SetTexture("_BumpMap",normalMap); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            material.EnableKeyword("_METALLICGLOSSMAP"); material.SetFloat("_GlossMapScale",.72f);
            material.enableInstancing=true; EditorUtility.SetDirty(material);
            Directory.CreateDirectory("Assets/Resources/Characters");
            var root=new GameObject("Arima");
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source),root.transform);
            model.name="Armored Samurai";
            var walk=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().Single(c=>c.name=="walk");
            walk.SampleAnimation(model,0);
            var bounds=new Bounds(); bool first=true; int triangles=0;
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterial=material; renderer.updateWhenOffscreen=true;
                var mesh=new Mesh(); renderer.BakeMesh(mesh);
                foreach(Vector3 vertex in mesh.vertices)
                {
                    Vector3 point=renderer.transform.TransformPoint(vertex);
                    if(first) { bounds=new Bounds(point,Vector3.zero); first=false; } else bounds.Encapsulate(point);
                }
                triangles+=mesh.triangles.Length/3; Object.DestroyImmediate(mesh);
            }
            // Normalize the actual deformed surface, not the FBX's oversized animation culling bounds.
            float scale=1.9f/bounds.size.y;
            model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            var rig=root.AddComponent<Ganryujima.World.SamuraiRig>(); rig.Model=model; rig.Walk=walk;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Characters/Arima.prefab");
            Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
            Debug.Log($"SAMURAI PREPARE PASS: {triangles} triangles, textures {color.width}px, source bounds {bounds}, normalized scale {scale}");
        }
        public static void PrepareAndCapture()
        {
            Prepare();
            VisualCapture.Run();
        }
        public static void Inspect()
        {
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Source);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.isReadable = false;
            importer.SaveAndReimport();
            Directory.CreateDirectory("Assets/Art/Samurai/Textures");
            importer.ExtractTextures("Assets/Art/Samurai/Textures");
            AssetDatabase.Refresh();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            var report = new StringBuilder();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                report.AppendLine($"BONE {t.name} position={t.localPosition} rotation={t.localEulerAngles} scale={t.localScale}");
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                report.AppendLine($"MESH {renderer.name} bounds={renderer.bounds} vertices={renderer.sharedMesh.vertexCount} bones={renderer.bones.Length}");
                foreach (Material m in renderer.sharedMaterials)
                {
                    report.AppendLine($"MAT {m.name} shader={m.shader.name}");
                    foreach(string property in m.GetTexturePropertyNames())
                        report.AppendLine($"TEXTURE {property}: {m.GetTexture(property)?.name} path={AssetDatabase.GetAssetPath(m.GetTexture(property))}");
                }
            }
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>())
                report.AppendLine($"CLIP {clip.name} duration={clip.length}");
            File.WriteAllText("../samurai-inspect.txt", report.ToString());
            Debug.Log("SAMURAI IMPORT INSPECT PASS");
            EditorApplication.Exit(0);
        }
    }
}
