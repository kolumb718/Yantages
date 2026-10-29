using UnityEngine;

// Humanoid legs finish in their own sagittal planes: knees bend forwards, not outwards.
// A stance foot stays at its planted world position; the swing foot follows a low arc.
// This is presentation on the existing flat decks, not a physics locomotion controller.
public sealed class CourierLegPlacement
{
    sealed class Leg
    {
        public Transform hip,knee,foot;
        public float thigh,shin,height,side;
        public Quaternion rotation;
        public Vector3 toeDirection;
        public Vector3 planted,swingStart;
        public Vector3 target,solved;
        public bool stance,initialized;
        public float phase;
    }
    readonly Transform courier;
    readonly Animator animator;
    readonly Transform pelvis;
    readonly Leg left,right;
    Vector3 previousPosition;
    Quaternion previousRotation;
    float weight;
    public float Weight => weight;
    public bool LeftPlanted => weight>.95f && left.stance;
    public bool RightPlanted => weight>.95f && right.stance;
    public Vector3 LeftTarget => left.target;
    public Vector3 LeftSolved => left.solved;

    public CourierLegPlacement(Transform root,Animator rig)
    {
        courier=root;animator=rig;pelvis=rig.GetBoneTransform(HumanBodyBones.Hips);
        left=Create(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,-1);
        right=Create(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes,1);
        float leftSlope=left.toeDirection.y/Mathf.Max(.01f,new Vector2(left.toeDirection.x,left.toeDirection.z).magnitude);
        float rightSlope=right.toeDirection.y/Mathf.Max(.01f,new Vector2(right.toeDirection.x,right.toeDirection.z).magnitude);
        float slope=Mathf.Abs(leftSlope)<Mathf.Abs(rightSlope)?leftSlope:rightSlope;
        float soleHeight=Mathf.Min(left.height,right.height);
        foreach(var leg in new[]{left,right})
        {
            float length=new Vector2(leg.toeDirection.x,leg.toeDirection.z).magnitude;
            leg.rotation=Quaternion.FromToRotation(leg.toeDirection,new Vector3(0,slope*length,length))*leg.rotation;
            leg.height=soleHeight;
        }
        previousPosition=courier.position;previousRotation=courier.rotation;
    }

    Leg Create(HumanBodyBones upper,HumanBodyBones lower,HumanBodyBones foot,HumanBodyBones toes,float side)
    {
        var leg=new Leg{hip=animator.GetBoneTransform(upper),knee=animator.GetBoneTransform(lower),
            foot=animator.GetBoneTransform(foot),side=side};
        leg.thigh=Vector3.Distance(leg.hip.position,leg.knee.position);
        leg.shin=Vector3.Distance(leg.knee.position,leg.foot.position);
        leg.height=Mathf.Clamp(leg.foot.position.y-courier.position.y,.08f,.19f);
        var toe=animator.GetBoneTransform(toes);
        var toeDirection=toe!=null?courier.InverseTransformVector(toe.position-leg.foot.position):Vector3.forward;
        leg.toeDirection=toeDirection;
        leg.rotation=Quaternion.Inverse(courier.rotation)*leg.foot.rotation;
        return leg;
    }

    public void Update(float speed,float dt)
    {
        bool relocated=(courier.position-previousPosition).magnitude>.5f
            || Quaternion.Angle(previousRotation,courier.rotation)>8;
        previousPosition=courier.position;previousRotation=courier.rotation;
        if(relocated){left.initialized=right.initialized=false;}
        float target=speed>.12f && !relocated?1:0;
        weight=Mathf.MoveTowards(weight,target,dt*10);
        if(weight<=0){left.initialized=right.initialized=false;return;}
        float run=Mathf.InverseLerp(CourierMotor.WalkSpeed,CourierMotor.SprintSpeed,speed);
        float stance=Mathf.Lerp(.55f,.38f,run);
        float cycle=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float length=Mathf.Max(.1f,animator.GetCurrentAnimatorStateInfo(0).length)/Mathf.Max(.1f,animator.speed);
        float lead=Mathf.Clamp(speed*length*stance*.5f,.12f,.5f);
        // A small knee softness keeps straight stance legs within their anatomical reach.
        pelvis.position-=Vector3.up*(.065f*weight);
        Place(left,Mathf.Repeat(cycle,1),stance,lead,Mathf.Lerp(.10f,.21f,run));
        Place(right,Mathf.Repeat(cycle+.5f,1),stance,lead,Mathf.Lerp(.10f,.21f,run));
    }

    void Place(Leg leg,float phase,float stance,float lead,float lift)
    {
        bool planted=phase<stance;
        var landing=courier.position+courier.right*(leg.side*.105f)+courier.forward*lead+Vector3.up*leg.height;
        var localAnchor=courier.InverseTransformPoint(leg.planted);
        bool reset=!leg.initialized || Mathf.Abs(localAnchor.x-leg.side*.105f)>.25f
            || Mathf.Abs(leg.planted.y-(courier.position.y+leg.height))>.15f;
        if(reset)
        {
            leg.planted=landing-courier.forward*(lead*2*Mathf.Clamp01(phase/stance));
            // Start from the last displayed stance, never from the Animator's uncorrected
            // turned-out foot pose; otherwise every toe-off reintroduces lateral motion.
            var local=courier.InverseTransformPoint(leg.foot.position);local.x=leg.side*.105f;
            leg.swingStart=courier.TransformPoint(local);leg.solved=leg.swingStart;
            leg.stance=planted;leg.initialized=true;
        }
        if(planted && !leg.stance)leg.planted=landing;
        if(!planted && leg.stance)leg.swingStart=leg.solved;
        Vector3 target;
        if(planted)target=leg.planted;
        else
        {
            float t=Mathf.Clamp01((phase-stance)/(1-stance));
            target=Vector3.Lerp(leg.swingStart,landing,Mathf.SmoothStep(0,1,t));
            target.y=Mathf.Lerp(leg.swingStart.y,landing.y,t)+Mathf.Sin(t*Mathf.PI)*lift;
        }
        leg.stance=planted;leg.phase=phase;
        leg.target=target;
        Solve(leg,Vector3.Lerp(leg.foot.position,target,weight));
        leg.foot.rotation=Quaternion.Slerp(leg.foot.rotation,courier.rotation*leg.rotation,weight);
        leg.solved=leg.foot.position;
    }

    void Solve(Leg leg,Vector3 target)
    {
        var hip=leg.hip.position;var delta=target-hip;
        float distance=Mathf.Clamp(delta.magnitude,.02f,leg.thigh+leg.shin-.001f);
        var direction=delta.sqrMagnitude>.00001f?delta.normalized:Vector3.down;
        target=hip+direction*distance;
        var pole=Vector3.ProjectOnPlane(courier.forward,direction).normalized;
        if(pole.sqrMagnitude<.1f)pole=courier.up;
        float along=(leg.thigh*leg.thigh-leg.shin*leg.shin+distance*distance)/(2*distance);
        float height=Mathf.Sqrt(Mathf.Max(0,leg.thigh*leg.thigh-along*along));
        var knee=hip+direction*along+pole*height;
        leg.hip.rotation=Quaternion.FromToRotation(leg.knee.position-hip,knee-hip)*leg.hip.rotation;
        leg.knee.rotation=Quaternion.FromToRotation(leg.foot.position-leg.knee.position,target-leg.knee.position)*leg.knee.rotation;
    }
}
