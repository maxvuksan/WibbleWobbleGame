using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderCircle : CustomCollider
{

    [SerializeField] private float _radius;
    VoltCircle _circle;


    public override void ConstructShape()
    {
        Fix64 radiusFix64 = (Fix64)_radius; 

        Vector3 finalPos = transform.TransformPoint(new Vector3(Offset.x, Offset.y, 0));
        VoltVector2 finalPosFix64 = new VoltVector2((Fix64)finalPos.x, (Fix64)finalPos.y);

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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(Offset.x, Offset.y, 0), _radius);

        DrawPositionDots();
    }
}
