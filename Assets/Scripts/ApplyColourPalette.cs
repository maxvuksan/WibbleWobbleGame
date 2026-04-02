using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies 
/// </summary>
public class ApplyColourPalette : MonoBehaviour
{

    [System.Serializable]
    public class ColourData
    {
        public ColourTarget ColourTarget;
        public int ColourTargetIndexOffset = 0;
    }

    [System.Serializable]
    public struct SpriteRendererToSetColour
    {
        public SpriteRenderer[] SpriteRenderers;
        public Graphic[] Graphics;
        public ColourData ColourData;
    }


    [SerializeField] private SpriteRendererToSetColour[] _targets;


    void Start()
    {
        ColourPaletteManager.Singleton.OnColourPaletteChange += ApplyColourData;
        ApplyColourData();
    }

    void OnDestroy() {
        ColourPaletteManager.Singleton.OnColourPaletteChange -= ApplyColourData;
    }

    void ApplyColourData()
    {
        for(int i = 0; i < _targets.Length; i++)
        {
            Color colour = ColourPaletteManager.Singleton.GetColour(_targets[i].ColourData.ColourTarget, _targets[i].ColourData.ColourTargetIndexOffset);
            for(int s = 0; s < _targets[i].SpriteRenderers.Length; s++)
            {
                _targets[i].SpriteRenderers[s].color = colour;
            }
            for(int s = 0; s < _targets[i].Graphics.Length; s++)
            {
                _targets[i].Graphics[s].color = colour;
            }
        }
    }
}
