using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Story
{
    /// <summary>Camera-only narrative shots; duel state and character transforms are never advanced.</summary>
    public sealed class StoryCamera
    {
        private readonly Camera view;
        private readonly Transform target,sword;
        private Vector3 savedPosition,shotPosition,from;
        private Quaternion savedRotation,shotRotation,fromRotation;
        private float savedFov;
        private bool mounted, swordVisible;
        public StoryCamera(Camera view,Transform target,Transform sword) { this.view=view; this.target=target; this.sword=sword; }
        public void Enter(StoryNode node)
        {
            if(!mounted)
            {
                savedPosition=view.transform.localPosition; savedRotation=view.transform.localRotation;
                savedFov=view.fieldOfView; swordVisible=sword.gameObject.activeSelf; mounted=true;
            }
            from=view.transform.position; fromRotation=view.transform.rotation;
            sword.gameObject.SetActive(false);
            Vector3 lookAt;
            if(node.shot=="opponent")
            {
                shotPosition=target.position+target.forward*3.2f+target.right*1.1f+Vector3.up*1.55f;
                lookAt=target.position+Vector3.up*1.1f;
            }
            else if(node.kind=="meditation") { shotPosition=new Vector3(18,1.1f,8); lookAt=new Vector3(55,.2f,45); }
            else if(node.shot=="shore") { shotPosition=new Vector3(7,1.8f,1); lookAt=new Vector3(45,.5f,40); }
            else { shotPosition=new Vector3(-4,2.3f,-6); lookAt=target.position+Vector3.up*.9f; }
            shotRotation=Quaternion.LookRotation(lookAt-shotPosition);
        }
        public void Tick(float age)
        {
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/GameConfig.StoryCameraBlend));
            view.transform.position=Vector3.Lerp(from,shotPosition,blend);
            view.transform.rotation=Quaternion.Slerp(fromRotation,shotRotation,blend);
            view.fieldOfView=Mathf.Lerp(savedFov,57,blend);
        }
        public void Restore()
        {
            if(!mounted) return;
            view.transform.localPosition=savedPosition; view.transform.localRotation=savedRotation; view.fieldOfView=savedFov;
            sword.gameObject.SetActive(swordVisible); mounted=false;
        }
    }
}
