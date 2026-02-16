using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderRectangle : CustomCollider
{

    [SerializeField] private Vector2 _size;
    private VoltPolygon _rect;


    public override void ConstructShape()
    {
        _size.x = Mathf.Abs(_size.x);
        _size.y = Mathf.Abs(_size.y);

        VoltVector2 half = new VoltVector2((Fix64)(_size.x / 2.0f), (Fix64)(_size.y / 2.0f)); 
        
        VoltVector2 offset = new VoltVector2(
            (Fix64)Offset.x,
            (Fix64)Offset.y
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(Offset.x, Offset.y, 0), new Vector3(_size.x, _size.y, 1));

        DrawPositionDots();
    }

    
}
