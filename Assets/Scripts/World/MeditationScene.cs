using UnityEngine;
using UnityEngine.Rendering;
using Ganryujima.Core;
using Ganryujima.Story;

namespace Ganryujima.World
{
    public sealed class MeditationScene
    {
        private readonly GameObject root;
        private readonly Material ink;
        private readonly AudioSource bell;
        private readonly AudioClip tone;
        public bool Visible => root.activeSelf;
        public MeditationScene(Camera camera)
        {
            root=new GameObject("Meditation enso"); root.transform.SetParent(camera.transform,false);
            root.transform.localPosition=new Vector3(0,.25f,2);
            root.transform.localScale=Vector3.one*.56f;
            var owner=root.AddComponent<GeneratedResources>();
            var mesh=owner.Own(new Mesh { name="Enso canvas",vertices=new[] { new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0) },
                uv=new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up },triangles=new[] {0,2,1,0,3,2} });
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            ink=owner.Own(new Material(Resources.Load<Shader>("Shaders/Enso")));
            var renderer=root.AddComponent<MeshRenderer>(); renderer.sharedMaterial=ink;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            bell=root.AddComponent<AudioSource>(); bell.playOnAwake=false; bell.spatialBlend=0;
            tone=Resources.Load<AudioClip>("Audio/Meditation/SingingBowl");
            root.SetActive(false);
        }
        public void Show(bool visible)
        {
            if(visible!=Visible) { bell.Stop(); root.SetActive(visible); }
        }
        public void Tick(MeditationModel model,bool paused)
        {
            ink.SetFloat("_Progress",model.Progress);
            ink.SetFloat("_Broken",model.BrokenRemaining/GameConfig.MeditationBreakFlash);
            ink.SetFloat("_Complete",model.Completed?1:0);
            if(paused) bell.Pause(); else bell.UnPause();
        }
        public void Complete() { if(tone!=null) bell.PlayOneShot(tone,GameConfig.MeditationBellVolume); }
    }
}
