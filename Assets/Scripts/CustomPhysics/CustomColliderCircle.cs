using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderCircle : CustomCollider
{

    [SerializeField] private IntHundredth _radius;
    VoltCircle _circle;


    public override void ConstructShape()
    {
        Fix64 radiusFix64 = (Fix64)_radius;

        VoltVector2 finalPosFix64 = Helpers.TransformPointFix64(GetCustomTransform(), Offset);

        _circle = new VoltCircle();
        _circle.InitializeFromWorldSpace(finalPosFix64, radiusFix64, (Fix64)1, (Fix64)1, (Fix64)1);
    }

    public override VoltShape GetShape()
    {
        return _circle;
    }

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        
        SetGizmoColourDependingIfTriggerOrNot();
        Gizmos.DrawWireSphere(new Vector3((float)OffsetX.AsFloat(), (float)OffsetY.AsFloat(), 0), (float)_radius.AsFloat());

        DrawPositionDots();
    }
}
