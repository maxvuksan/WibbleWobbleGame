using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderPolygon : CustomCollider
{
    
    [SerializeField] private IntHundredthVector2[] _points;
    private VoltPolygon _polygon;

    public override void ConstructShape()
    {
        List<VoltVector2> verts = new();

        for(int i = 0; i < _points.Length; i++)
        {
            verts.Add(new VoltVector2((Fix64)_points[i].X, (Fix64)_points[i].Y));
        }

        _polygon = new VoltPolygon();
        _polygon.InitializeFromBodyVertices(verts.ToArray(), (Fix64)1, (Fix64)1, (Fix64)1);
    }

    public override VoltShape GetShape()
    {
        return _polygon;
    }

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        SetGizmoColourDependingIfTriggerOrNot();

        if(_points != null)
        {
            for(int i = 0; i < _points.Length; i++)
            {
                int nextIndex = (i + 1) % _points.Length;
                Vector2 pointA = new Vector2(_points[i].X.AsFloat(), _points[i].Y.AsFloat());
                Vector2 pointB = new Vector2(_points[nextIndex].X.AsFloat(), _points[nextIndex].Y.AsFloat());
                Vector2 offset = new Vector2(OffsetX.AsFloat(), OffsetY.AsFloat());
                Gizmos.DrawLine(pointA + offset, pointB + offset);
            }
        }

        DrawPositionDots();
    }

}
