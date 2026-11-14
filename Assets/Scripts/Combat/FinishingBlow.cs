using UnityEngine;
using Ganryujima.Core;
using Ganryujima.World;

namespace Ganryujima.Combat
{
    /// <summary>First-person finishing cut, followed by a held result pose. No extra damage is applied.</summary>
    public sealed class FinishingBlow
    {
        public FinisherTimeline Timeline { get; } = new FinisherTimeline();
        public bool Active => Timeline.Active;
        public bool Paused { get; private set; }
        public float Flash => Mathf.Clamp01(1-Mathf.Abs(Timeline.Elapsed-GameConfig.FinisherImpactAt)/.10f)*.32f;
        private readonly Camera camera;
        private readonly Transform sword, target;
        private readonly SamuraiRig rig;
        private readonly CombatPresentation presentation;
        private readonly AudioSource sound;
        private readonly AudioClip slash;
        private Vector3 cameraStart, cameraLocal, swordPosition, direction, origin;
        private Quaternion cameraRotation, swordRotation;
        private float fov;
        private bool swordVisible;

        public FinishingBlow(Camera camera, Transform sword, Transform target, CombatPresentation presentation)
        {
            this.camera=camera; this.sword=sword; this.target=target; this.presentation=presentation;
            rig=target.GetComponent<SamuraiRig>();
            sound=camera.gameObject.AddComponent<AudioSource>(); sound.playOnAwake=false; sound.spatialBlend=0;
            slash=Resources.Load<AudioClip>("Audio/Finisher/SwordSlash");
        }
        public bool TryBegin(CombatModel combat)
        {
            if (!Timeline.Begin(combat)) return false;
            Paused=false;
            cameraStart=camera.transform.position; cameraLocal=camera.transform.localPosition;
            cameraRotation=camera.transform.localRotation; fov=camera.fieldOfView;
            swordPosition=sword.localPosition; swordRotation=sword.localRotation; swordVisible=sword.gameObject.activeSelf;
            origin=target.position;
            direction=Vector3.ProjectOnPlane(cameraStart-origin,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f) direction=-target.forward;
            presentation.Reset(); combat.CancelHeldInput();
            Pose();
            return true;
        }
        public void TogglePause()
        {
            Paused=!Paused;
            if(Paused) sound.Pause(); else sound.UnPause();
        }
        public void Tick(float dt, bool skip)
        {
            Timeline.Tick(dt,Paused,skip);
            if(Paused) return;
            if(Timeline.ImpactThisTick)
            {
                presentation.FinisherImpact();
                if(slash!=null) sound.PlayOneShot(slash,GameConfig.FinisherVolume);
            }
            presentation.Tick(dt,0);
            if(Active) Pose(); else Restore();
        }
        private void Pose()
        {
            float t=Timeline.Elapsed;
            float cut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(GameConfig.FinisherCutAt,GameConfig.FinisherCutAt+GameConfig.FinisherCutDuration,t));
            float fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(GameConfig.FinisherImpactAt+.1f,2.8f,t));
            Vector3 side=Vector3.Cross(Vector3.up,direction);
            Vector3 close=origin+direction*1.85f+side*.18f+Vector3.up*1.63f;
            Vector3 follow=origin+direction*2.2f+side*.65f+Vector3.up*1.4f;
            float approach=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.75f));
            camera.transform.position=Vector3.Lerp(cameraStart,Vector3.Lerp(close,follow,fall),approach)+presentation.CameraOffset;
            Vector3 lookAt=origin+Vector3.up*Mathf.Lerp(1.35f,.75f,fall);
            Quaternion look=Quaternion.LookRotation(lookAt-camera.transform.position);
            camera.transform.rotation=Quaternion.Slerp(camera.transform.parent.rotation*cameraRotation,look,approach);
            camera.fieldOfView=Mathf.Lerp(fov,55,approach);
            rig.PoseFinisher(fall);
            Vector3 readyPosition=new Vector3(.42f,-.22f,.70f);
            sword.localPosition=Vector3.Lerp(swordPosition,Vector3.Lerp(readyPosition,new Vector3(-.38f,-.5f,.9f),cut),approach);
            sword.localRotation=Quaternion.Slerp(swordRotation,Quaternion.Slerp(Quaternion.Euler(-45,-15,-50),Quaternion.Euler(100,20,55),cut),approach);
            sword.gameObject.SetActive(swordVisible && t<2.4f);
        }
        private void Restore()
        {
            camera.transform.localPosition=cameraLocal; camera.transform.localRotation=cameraRotation; camera.fieldOfView=fov;
            sword.localPosition=swordPosition; sword.localRotation=swordRotation; sword.gameObject.SetActive(swordVisible);
            rig.PoseFinisher(1);
            sound.Stop(); presentation.Reset();
        }
        public void Reset()
        {
            if(Timeline.Started) Restore();
            Timeline.Reset(); Paused=false;
        }
    }
}
