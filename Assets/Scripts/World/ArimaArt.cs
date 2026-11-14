using UnityEngine;
using Ganryujima.Combat;

namespace Ganryujima.World
{
    /// <summary>Locally authored clothed duelist with articulated tell, cut, recoil and yield poses.</summary>
    public sealed class ArimaArt
    {
        public Transform Root { get; }
        private readonly Transform body, weapon, leftArm, rightArm, leftLeg, rightLeg, leftForearm, rightForearm;
        private Vector3 previous;
        private float stride;

        public ArimaArt(Transform parent)
        {
            Root = new GameObject("Arima Kihei").transform; Root.SetParent(parent, false);
            var owner = Root.gameObject.AddComponent<GeneratedResources>();
            Material cloth = ArtMaterials.Surface(owner, "Weathered indigo kosode", new Color(.13f,.19f,.21f), "cloth");
            Material trousers = ArtMaterials.Surface(owner, "Charcoal hakama", new Color(.09f,.11f,.12f), "cloth");
            Material skin = ArtMaterials.Surface(owner, "Warm skin", new Color(.57f,.40f,.29f), "stone", .22f);
            Material hair = ArtMaterials.Surface(owner, "Black hair", new Color(.035f,.03f,.026f), null);
            Material sash = ArtMaterials.Surface(owner, "Worn vermilion obi", new Color(.34f,.12f,.08f), "cloth");
            Material linen = ArtMaterials.Surface(owner, "Undershirt and tabi", new Color(.67f,.64f,.54f), "cloth");
            body = new GameObject("Breath and recoil").transform; body.SetParent(Root, false);
            body.localPosition = Vector3.up * .94f;
            Garment("Kosode torso", body, owner, cloth, new Vector3(0,.02f,0), .57f, .25f,.21f,.15f);
            Garment("Hakama pleats", body, owner, trousers, new Vector3(0,-.68f,0), .75f,.38f,.23f,.22f);
            Garment("Obi", body, owner, sash, Vector3.zero, .11f,.245f,.245f,.17f);
            var collarA = Part("Crossed collar", body, PrimitiveType.Cube, new Vector3(-.055f,.42f,.15f), new Vector3(.045f,.32f,.025f), linen);
            collarA.localRotation = Quaternion.Euler(0,0,27);
            var collarB = Part("Crossed lapel", body, PrimitiveType.Cube, new Vector3(.055f,.40f,.17f), new Vector3(.065f,.35f,.025f), cloth);
            collarB.localRotation = Quaternion.Euler(0,0,-27);
            Part("Neck", body, PrimitiveType.Cylinder, new Vector3(0,.64f,0), new Vector3(.12f,.09f,.12f), skin);
            Part("Head", body, PrimitiveType.Sphere, new Vector3(0,.79f,0), new Vector3(.235f,.29f,.235f), skin);
            Part("Hair crown", body, PrimitiveType.Sphere, new Vector3(0,.88f,-.025f), new Vector3(.244f,.16f,.23f), hair);
            Part("Topknot", body, PrimitiveType.Capsule, new Vector3(0,.95f,-.05f), new Vector3(.075f,.06f,.075f), hair);
            Part("Nose", body, PrimitiveType.Sphere, new Vector3(0,.78f,.119f), new Vector3(.041f,.065f,.045f), skin);
            for (int i = -1; i <= 1; i += 2)
            {
                Part("Brow", body, PrimitiveType.Cube, new Vector3(i*.05f,.826f,.107f), new Vector3(.057f,.009f,.016f), hair);
                Part("Eye", body, PrimitiveType.Sphere, new Vector3(i*.05f,.804f,.11f), new Vector3(.026f,.012f,.012f), hair);
                Part("Ear", body, PrimitiveType.Sphere, new Vector3(i*.117f,.78f,0), new Vector3(.035f,.07f,.04f), skin);
            }
            Part("Mouth", body, PrimitiveType.Cube, new Vector3(0,.722f,.106f), new Vector3(.05f,.009f,.012f), hair);
            leftArm = Arm("Left sleeve", -.26f, body, owner, cloth, skin);
            rightArm = Arm("Right sleeve", .26f, body, owner, cloth, skin);
            leftForearm = Part("Left forearm", body, PrimitiveType.Capsule, Vector3.zero, Vector3.one, skin);
            rightForearm = Part("Right forearm", body, PrimitiveType.Capsule, Vector3.zero, Vector3.one, skin);
            leftLeg = Part("Left tabi", Root, PrimitiveType.Capsule, new Vector3(-.16f,.12f,.04f), new Vector3(.13f,.11f,.25f), linen);
            rightLeg = Part("Right tabi", Root, PrimitiveType.Capsule, new Vector3(.16f,.12f,-.09f), new Vector3(.13f,.11f,.25f), linen);
            weapon = new GameObject("Two handed sword pose").transform; weapon.SetParent(body, false);
            Transform blade = KatanaArt.Build(weapon, false); blade.localPosition = Vector3.zero; blade.localRotation = Quaternion.identity;
            previous = new Vector3(0,0,2);
        }
        private static Transform Part(string name, Transform parent, PrimitiveType type, Vector3 p, Vector3 scale, Material mat)
            => ArenaBuilder.Primitive(name,type,parent,p,scale,mat);
        private static Transform Arm(string name, float x, Transform parent, GeneratedResources owner, Material cloth, Material skin)
        {
            var joint = new GameObject(name).transform; joint.SetParent(parent,false); joint.localPosition = new Vector3(x,.49f,0);
            Garment(name, joint, owner, cloth, new Vector3(0,-.37f,0), .38f,.13f,.15f,.13f);

            return joint;
        }
        private static void Garment(string name, Transform parent, GeneratedResources owner, Material mat, Vector3 p, float height, float bottom, float top, float depth)
        {
            const int count = 48;
            var v = new Vector3[(count+1)*2]; var uv = new Vector2[v.Length]; var triangles = new int[count*6];
            for (int row=0;row<2;row++) for(int i=0;i<=count;i++)
            {
                float a=i*Mathf.PI*2/count, fold=1+Mathf.Cos(a*12)*.055f;
                int k=row*(count+1)+i;
                v[k]=new Vector3(Mathf.Cos(a)*(row==0?bottom:top)*fold,row*height,Mathf.Sin(a)*depth*fold);
                uv[k]=new Vector2(i/(float)count,row);
                if(row==0 && i<count) { int t=i*6; triangles[t]=i; triangles[t+1]=i+count+1; triangles[t+2]=i+1; triangles[t+3]=i+1;triangles[t+4]=i+count+1;triangles[t+5]=i+count+2; }
            }
            var mesh=owner.Own(new Mesh { name=name, vertices=v, uv=uv, triangles=triangles });mesh.RecalculateNormals();mesh.RecalculateBounds();
            ArenaBuilder.MeshObject(name,parent,mesh,mat).localPosition=p;
        }
        private void PoseArm(Transform upper, Transform lower, Vector3 handWorld, float side)
        {
            Vector3 hand = body.InverseTransformPoint(handWorld);
            Vector3 shoulder = upper.localPosition;
            Vector3 elbow = Vector3.Lerp(shoulder,hand,.52f) + new Vector3(side*.12f,-.12f,-.02f);
            Vector3 upperDelta = elbow-shoulder;
            upper.localRotation = Quaternion.FromToRotation(Vector3.down,upperDelta.normalized);
            upper.localScale = new Vector3(1,upperDelta.magnitude/.37f,1);
            Vector3 forearm = hand-elbow;
            lower.localPosition = (elbow+hand)*.5f;
            lower.localRotation = Quaternion.FromToRotation(Vector3.up,forearm.normalized);
            lower.localScale = new Vector3(.085f,forearm.magnitude*.5f+.015f,.085f);
        }
        public void Tick(ArimaOpponent ai, float dt)
        {
            Vector3 p = new Vector3(ai.Position.x,0,ai.Position.y);
            stride += Vector3.Distance(previous,p)*6; previous=p;
            Root.position=p; Root.rotation=Quaternion.LookRotation(new Vector3(ai.Forward.x,0,ai.Forward.y));
            bool walking=ai.State==OpponentState.Circle;
            float step=walking?Mathf.Sin(stride)*.11f:0;
            leftLeg.localPosition=new Vector3(-.16f,.12f+Mathf.Max(0,step)*.35f,.04f+step);
            rightLeg.localPosition=new Vector3(.16f,.12f+Mathf.Max(0,-step)*.35f,-.09f-step);
            float lean=ai.State==OpponentState.Recoil?-22:ai.State==OpponentState.Staggered?16:ai.CounterOpen?-10:0;
            bool yielded=ai.State==OpponentState.Yield;
            body.localPosition=Vector3.up*(yielded?.55f:.94f);
            body.localRotation=Quaternion.Euler(yielded?25:lean,0,0);
            Vector3 rest=new Vector3(30,0,-15), raised;
            switch(ai.Zone)
            {
                case Stance.Upper: raised=new Vector3(-75,0,5);break;
                case Stance.Lower: raised=new Vector3(65,0,-45);break;
                case Stance.Side: raised=new Vector3(5,-40,-80);break;
                default: raised=new Vector3(15,25,60);break;
            }
            Vector3 pose=rest;
            if(ai.Telling) pose=Vector3.Lerp(rest,raised,Mathf.Clamp01(ai.TellProgress*2));
            if(ai.State==OpponentState.Strike) pose=Vector3.Lerp(raised,new Vector3(105,0,40),Mathf.Clamp01(ai.StateAge/.22f));
            if(ai.State==OpponentState.Feint || ai.CounterOpen || ai.State==OpponentState.Recoil) pose=new Vector3(-35,0,-90);
            if(ai.State==OpponentState.Guard) pose=Vector3.Lerp(rest,raised,.5f);
            if(yielded) pose=new Vector3(100,0,30);
            weapon.localPosition=new Vector3(.04f,.35f,.43f);
            weapon.localRotation=Quaternion.Slerp(weapon.localRotation,Quaternion.Euler(pose),1-Mathf.Exp(-20*dt));
            PoseArm(leftArm, leftForearm, weapon.TransformPoint(new Vector3(-.025f,-.09f,.015f)), -1);
            PoseArm(rightArm, rightForearm, weapon.TransformPoint(new Vector3(.035f,.04f,.008f)), 1);
        }
    }
}
