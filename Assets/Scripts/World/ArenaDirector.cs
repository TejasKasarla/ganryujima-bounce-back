using System.Collections.Generic;
using UnityEngine;

namespace Ganryujima.World
{
    /// <summary>Switches arena dressing without replacing the player, opponent or face provider.</summary>
    public sealed class ArenaDirector
    {
        private readonly List<(GameObject item,bool active)> court=new List<(GameObject,bool)>();
        private readonly GameObject beach,grove,lanterns;
        private readonly Transform gate;
        private readonly Light sun;
        public string Current { get; private set; }="court";
        public ArenaDirector(Transform original)
        {
            foreach(Transform child in original) if(child.GetComponent<Light>()==null) court.Add((child.gameObject,child.gameObject.activeSelf));
            sun=RenderSettings.sun; gate=original.Find("Weathered shrine torii");
            beach=BuildBeach(); grove=new GameObject("Yoshioka grove dressing"); lanterns=new GameObject("Yoshioka lantern court dressing");
            Transform pine=original.Find("Wind-sculpted black pine");
            Transform lantern=original.Find("Stone lantern");
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10;
                var tree=Object.Instantiate(pine,grove.transform); tree.localPosition=new Vector3(Mathf.Sin(a)*19,0,Mathf.Cos(a)*19);
                tree.localScale=Vector3.one*(.65f+i%3*.12f); tree.localRotation=Quaternion.Euler(0,i*37,0);
                var lamp=Object.Instantiate(lantern,lanterns.transform); lamp.localPosition=new Vector3(Mathf.Sin(a)*14.5f,0,Mathf.Cos(a)*14.5f);
            }
            beach.SetActive(false); grove.SetActive(false); lanterns.SetActive(false);
        }
        public void Show(string arena)
        {
            if(string.IsNullOrEmpty(arena)) arena="court";
            Current=arena;
            bool onBeach=arena.StartsWith("beach");
            foreach(var item in court) item.item.SetActive(!onBeach && item.active);
            beach.SetActive(onBeach); grove.SetActive(arena=="grove"); lanterns.SetActive(arena=="lantern");
            if(gate!=null) gate.gameObject.SetActive(!onBeach && arena!="grove");
            RenderSettings.fogDensity=arena=="grove"?.018f:arena=="lantern"?.014f:.008f;
            RenderSettings.fogColor=arena=="lantern"?new Color(.43f,.46f,.43f):new Color(.73f,.74f,.68f);
            sun.intensity=arena=="lantern"?.58f:1.35f;
            sun.color=arena=="beach_late"?new Color(1,.78f,.52f):new Color(1,.88f,.67f);
        }
        private static GameObject BuildBeach()
        {
            var root=new GameObject("Ganryujima beach"); var owner=root.AddComponent<GeneratedResources>();
            var sand=ArtMaterials.Surface(owner,"Poly Haven coast sand",new Color(.9f,.87f,.78f),null,.12f);
            sand.mainTexture=Resources.Load<Texture2D>("Art/Coast/SandColor"); sand.mainTextureScale=Vector2.one*.75f;
            sand.SetTexture("_BumpMap",Resources.Load<Texture2D>("Art/Coast/SandNormal")); sand.EnableKeyword("_NORMALMAP"); sand.SetFloat("_BumpScale",.55f);
            const int side=101;
            var v=new Vector3[side*side]; var uv=new Vector2[v.Length]; var indices=new List<int>();
            for(int z=0;z<side;z++) for(int x=0;x<side;x++)
            {
                int i=z*side+x; float px=(x-50)*2,pz=(z-50)*2;
                float edge=Mathf.SmoothStep(0,1,(new Vector2(px,pz).magnitude-13.5f)/12);
                float height=pz>13.5f?-(pz-13.5f)*.14f:edge*Mathf.PerlinNoise(px*.09f+20,pz*.09f+40)*1.9f;
                v[i]=new Vector3(px,height,pz); uv[i]=new Vector2(px,pz)*.35f;
                if(x==100 || z==100) continue;
                indices.Add(i); indices.Add(i+side); indices.Add(i+1); indices.Add(i+1); indices.Add(i+side); indices.Add(i+side+1);
            }
            var mesh=owner.Own(ArtGeometry.Finish("Sloping Ganryujima shore",v,indices.ToArray(),uv));
            var ground=ArenaBuilder.MeshObject("Sand shore",root.transform,mesh,sand);
            ground.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
            var water=owner.Own(new Material(Resources.Load<Shader>("Shaders/CoastalWater")));
            ArenaBuilder.Primitive("Open sea",PrimitiveType.Plane,root.transform,new Vector3(0,-.3f,230),new Vector3(80,1,43),water);
            var rock=ArtMaterials.Surface(owner,"Sea granite",new Color(.29f,.32f,.30f),"stone",.25f);
            var stone=owner.Own(ArtGeometry.Rock(78));
            for(int i=0;i<14;i++)
            {
                var r=ArenaBuilder.MeshObject("Shore outcrop",root.transform,stone,rock);
                r.localPosition=new Vector3((i%2==0?-1:1)*(17+i*1.3f),-.15f,4+i*2);
                r.localScale=new Vector3(1.7f+i%3,1+i%2*.8f,2+i%4*.6f); r.localRotation=Quaternion.Euler(0,i*31,i%3*5);
            }
            var wood=ArtMaterials.Surface(owner,"Salt worn driftwood",new Color(.35f,.29f,.21f),"wood");
            for(int i=0;i<6;i++)
                ArenaBuilder.Tube("Bleached driftwood",root.transform,owner,new[] {new Vector3(-18-i*1.8f,.2f,-3+i*2),new Vector3(-16-i*1.8f,.4f,-2+i*2),new Vector3(-13-i*1.8f,.15f,-1+i*2)},.19f,.09f,wood,9);
            return root;
        }
    }
}
