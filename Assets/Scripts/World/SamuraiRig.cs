using System.Collections.Generic;
using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;

namespace Ganryujima.World
{
    /// <summary>Imported skinned character; authored walk plus two-bone sword posing on the combat clock.</summary>
    public sealed class SamuraiRig : MonoBehaviour
    {
        public GameObject Model;
        public AnimationClip Walk;
        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private Transform sword;
        private float clock, walkTime;
        private Vector3 lastPosition;
        private Quaternion swordRotation = Quaternion.Euler(25, 0, -12);
        private Vector3 swordPosition = new Vector3(.04f, 1.18f, .43f);
        private Vector3 modelOrigin;
        private Material originalArmor;
        private readonly Dictionary<OpponentId,Material> variants=new Dictionary<OpponentId,Material>();
        private Vector3 modelScale;
        private readonly Transform[] fingers = new Transform[24];
        private Limb leftLeg, rightLeg, leftArm, rightArm;
        private Transform hips, head;
        private AnimationClip sampledWalk;
        private struct Limb { public Transform upper, lower, end; }
        private Limb CacheLimb(string side, string upper, string lower, string end) => new Limb
            { upper = bones[side + upper], lower = bones[side + lower], end = bones[side + end] };

        public void Initialize()
        {
            foreach (Transform bone in Model.GetComponentsInChildren<Transform>()) bones[bone.name] = bone;
            hips = bones["Hips"]; head = bones["Head"];
            leftLeg = CacheLimb("Left", "UpLeg", "Leg", "Foot"); rightLeg = CacheLimb("Right", "UpLeg", "Leg", "Foot");
            leftArm = CacheLimb("Left", "Arm", "ForeArm", "Hand"); rightArm = CacheLimb("Right", "Arm", "ForeArm", "Hand");
            int index = 0;
            foreach (string side in new[] { "Left", "Right" })
                foreach (string finger in new[] { "Index", "Middle", "Ring", "Pinky" })
                    for (int joint = 1; joint <= 3; joint++) fingers[index++] = bones[side + "Hand" + finger + joint];
            foreach (var animator in Model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            // SampleAnimation without an Animator is editor-only for Generic clips. A private legacy
            // copy keeps these transform curves sampleable on our paused/slowed clock in the player.
            var resourceOwner = GetComponent<GeneratedResources>() ?? gameObject.AddComponent<GeneratedResources>();
            sampledWalk = resourceOwner.Own(Instantiate(Walk)); sampledWalk.legacy = true;
            modelOrigin = Model.transform.localPosition;
            modelScale=Model.transform.localScale;
            originalArmor=Model.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
            sword = KatanaArt.Build(transform, false, false);
            lastPosition = transform.position;
        }
        public void SetIdentity(OpponentId id)
        {
            if(!variants.TryGetValue(id,out var material))
            {
                material=originalArmor;
                if(id!=OpponentId.Arima)
                {
                    var owner=GetComponent<GeneratedResources>()??gameObject.AddComponent<GeneratedResources>();
                    material=owner.Own(new Material(originalArmor)); material.shader=Resources.Load<Shader>("Shaders/SamuraiLacquer");
                    material.SetColor("_ArmorTint",id==OpponentId.Kojiro?new Color(.8f,.82f,.72f):new Color(.065f,.11f,.14f));
                }
                variants[id]=material;
            }
            foreach(var renderer in Model.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.sharedMaterial=material;
            Model.transform.localScale=modelScale*(id==OpponentId.Kojiro?1.055f:1);
            sword.localScale=new Vector3(1,id==OpponentId.Kojiro?1.38f:1,1);
        }

        public void Pose(ArimaOpponent ai, float dt)
        {
            if (sword == null) Initialize();
            clock += dt;
            float travel = Vector3.Distance(lastPosition, transform.position);
            lastPosition = transform.position;
            if (ai.State == OpponentState.Circle) walkTime += travel / 1.4f;
            sampledWalk.SampleAnimation(Model, ai.State == OpponentState.Circle ? Mathf.Repeat(walkTime, sampledWalk.length) : 0);
            // Sampled clips supply feet and hips. Gameplay alone owns movement and facing.
            Model.transform.localPosition = modelOrigin;
            Model.transform.localRotation = Quaternion.identity;
            float lean = ai.State == OpponentState.Recoil ? -18 : ai.State == OpponentState.Staggered ? 18 : ai.CounterOpen ? -8 : 0;
            hips.rotation = Quaternion.AngleAxis(lean, transform.right) * hips.rotation;
            bool yielded = ai.State == OpponentState.Yield;
            if (yielded)
            {
                hips.position -= transform.up * .32f;
                hips.rotation = Quaternion.AngleAxis(24, transform.right) * hips.rotation;
            }
            if (ai.State != OpponentState.Circle)
            {
                PlantFoot("Left", new Vector3(-.15f,.12f,.08f));
                PlantFoot("Right", new Vector3(.15f,.12f,-.12f));
            }
            Vector3 rest = new Vector3(25, 0, -12), raised;
            Vector3 grip = new Vector3(.04f, 1.18f, .43f);
            switch (ai.Zone)
            {
                case Stance.Upper: raised = new Vector3(-65, 0, 0); break;
                case Stance.Lower: raised = new Vector3(65, 0, -40); break;
                case Stance.Side: raised = new Vector3(5, -35, -75); break;
                default: raised = new Vector3(10, 30, 55); break;
            }
            Vector3 pose = rest;
            if (ai.Telling)
            {
                float amount = Mathf.SmoothStep(0,1,Mathf.Clamp01(ai.TellProgress*2));
                pose = Vector3.Lerp(rest, raised, amount);
                if (ai.Zone == Stance.Upper) grip += new Vector3(0,.29f,-.07f)*amount;
            }
            if (ai.State == OpponentState.Strike)
                pose = Vector3.Lerp(raised,new Vector3(100,0,35),Mathf.Clamp01(ai.StateAge/GameConfig.ArimaStrike));
            if (ai.State == OpponentState.Feint || ai.CounterOpen || ai.State == OpponentState.Recoil) pose = new Vector3(-25,0,-75);
            if (ai.State == OpponentState.Guard) pose = Vector3.Lerp(rest,raised,.5f);
            if (yielded) { pose = new Vector3(100,0,30); grip.y -= .3f; }
            grip.y += Mathf.Sin(clock*2)*.006f;
            float blend = 1-Mathf.Exp(-22*dt);
            swordPosition = Vector3.Lerp(swordPosition,grip,blend);
            swordRotation = Quaternion.Slerp(swordRotation,Quaternion.Euler(pose),blend);
            sword.localPosition = swordPosition;
            sword.localRotation = swordRotation;
            Arm("Right", sword.TransformPoint(new Vector3(0,.04f,0)), 1);
            Arm("Left", sword.TransformPoint(new Vector3(0,-.09f,0)), -1);
            CurlFingers();
        }

        private void CurlFingers()
        {
            Quaternion curl = Quaternion.Euler(-45, 0, 0);
            for (int i = 0; i < fingers.Length; i++) fingers[i].localRotation *= curl;
        }

        public void PoseFinisher(float fall)
        {
            sampledWalk.SampleAnimation(Model,0);
            Model.transform.localPosition=modelOrigin; Model.transform.localRotation=Quaternion.identity;
            hips.position-=transform.up*(fall*.53f);
            hips.rotation=Quaternion.AngleAxis(fall*24,transform.right)*hips.rotation;
            PlantFoot("Left",new Vector3(-.17f,.10f,.12f));
            PlantFoot("Right",new Vector3(.17f,.10f,-.22f));
            head.rotation=Quaternion.AngleAxis(fall*18,transform.right)*head.rotation;
            sword.localPosition=new Vector3(.32f,Mathf.Lerp(1.05f,.40f,fall),.35f);
            sword.localRotation=Quaternion.Euler(65,20,-70);
            sword.gameObject.SetActive(true);
            Arm("Right",sword.TransformPoint(new Vector3(0,.04f,0)),1);
            Arm("Left",sword.TransformPoint(new Vector3(0,-.09f,0)),-1);
            CurlFingers();
        }

        private void PlantFoot(string side, Vector3 localTarget)
        {
            Limb limb = side == "Left" ? leftLeg : rightLeg;
            Transform upper = limb.upper, lower = limb.lower, foot = limb.end;
            Quaternion rotation=foot.rotation;
            Solve(upper,lower,foot,transform.TransformPoint(localTarget),transform.forward);
            foot.rotation=rotation;
        }

        private void Arm(string side, Vector3 target, float sign)
        {
            Limb limb = side == "Left" ? leftArm : rightArm;
            Transform upper = limb.upper, lower = limb.lower, hand = limb.end;
            Solve(upper,lower,hand,target,transform.TransformDirection(new Vector3(sign*.65f,-1,-.15f)));
            hand.rotation=sword.rotation*Quaternion.Euler(0,sign*90,90);
        }
        private static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint)
        {
            Vector3 origin=upper.position;
            float a=Vector3.Distance(origin,lower.position), b=Vector3.Distance(lower.position,end.position);
            Vector3 direction=(target-origin).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(target,origin),Mathf.Abs(a-b)+.001f,a+b-.001f);
            float along=(a*a-b*b+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(hint,direction).normalized;
            Vector3 elbow=origin+direction*along+bend*height;
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,target-lower.position)*lower.rotation;
        }
    }
}
