using Unity.VisualScripting;
using UnityEngine;
using Volatile;

public class RopeVisual : MonoBehaviour
{
    LineRenderer _lineRenderer;

    virtual public void Awake()
    {
        _lineRenderer = this.AddComponent<LineRenderer>();
        _lineRenderer.widthMultiplier = 0.3f;
        _lineRenderer.material = Helpers.Singleton.RopeMaterial;
        _lineRenderer.startColor = ColourPaletteManager.Singleton.GetColour(ColourTarget.COLOUR_PRIMARY, 0);
        _lineRenderer.endColor = ColourPaletteManager.Singleton.GetColour(ColourTarget.COLOUR_PRIMARY, 0);
    }

    public void SetPoint(int index, VoltVector2 point)
    {
        SetPoint(index, new Vector2((float)point.x, (float)point.y));
    }

    public void SetPoint(int index, Vector2 point)
    {
        if(index >= _lineRenderer.positionCount)
        {
            _lineRenderer.positionCount = index + 1;
        }

        _lineRenderer.SetPosition(index, point);
    }
}
