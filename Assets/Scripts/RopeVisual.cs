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
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true;
        OnColourPaletteChange();
        ColourPaletteManager.Singleton.OnColourPaletteChange += OnColourPaletteChange;
    }

    virtual public void OnDestroy() {
        ColourPaletteManager.Singleton.OnColourPaletteChange -= OnColourPaletteChange;
    }

    private void OnColourPaletteChange()
    {
        _lineRenderer.startColor = ColourPaletteManager.Singleton.GetColourPalette().ropeColour;
        _lineRenderer.endColor = ColourPaletteManager.Singleton.GetColourPalette().ropeColour;
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
