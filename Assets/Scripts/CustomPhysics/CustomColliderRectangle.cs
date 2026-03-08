using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderRectangle : CustomCollider
{

    [SerializeField] private IntHundredth _sizeX;
    [SerializeField] private IntHundredth _sizeY;
    private VoltPolygon _rect;


    public override void ConstructShape()
    {
        Fix64 sizeXFix64 =  Fix64.Abs((Fix64)_sizeX);
        Fix64 sizeYFix64 =  Fix64.Abs((Fix64)_sizeY);

        VoltVector2 half = new VoltVector2(sizeXFix64 / (Fix64)2, sizeYFix64 / (Fix64)2); 
        
        VoltVector2 offset = new VoltVector2(
            Offset.x,
            Offset.y
        );

        VoltVector2[] verts =
        {
            offset + new VoltVector2(-half.x, -half.y),
            offset + new VoltVector2(-half.x,  half.y),
            offset + new VoltVector2( half.x,  half.y),
            offset + new VoltVector2( half.x, -half.y),
        };

        _rect = new VoltPolygon();
        _rect.InitializeFromBodyVertices(verts, (Fix64)1, (Fix64)1, (Fix64)1);
    }

    public override VoltShape GetShape()
    {
        return _rect;
    }

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        SetGizmoColourDependingIfTriggerOrNot();
        Gizmos.DrawWireCube(new Vector3((float)OffsetX.AsFloat(), (float)OffsetY.AsFloat(), 0), new Vector3((float)_sizeX.AsFloat(), (float)_sizeY.AsFloat(), 1));

        DrawPositionDots();
    }

    
}
