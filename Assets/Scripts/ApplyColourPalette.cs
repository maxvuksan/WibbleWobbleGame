using UnityEngine;

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
        public ColourData ColourData;
    }


    [SerializeField] private SpriteRendererToSetColour[] _targets;


    void Start()
    {
        ColourPaletteManager.Singleton.OnColourPaletteChange += ApplyColourData;
        ApplyColourData();
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
        }
    }
}
