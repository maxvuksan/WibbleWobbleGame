using System;
using UnityEngine;


[System.Serializable]
public enum ColourTarget
{
    COLOUR_UNCHANGED,
    COLOUR_PRIMARY,
    COLOUR_BACKGROUND_SILOUTTES,
}

[System.Serializable]
public class ColourPalette{

    public Color[] primaryBlockColours;
    public Color backgroundColourSilouttes;
    public Color backgroundColourA;
    public Color backgroundColourB;
}

/// <summary>
/// ColourPaletteManager is responsible for changing the colours of traps, background and ui elements.
/// </summary>
public class ColourPaletteManager : MonoBehaviour
{
    [SerializeField] private ColourPaletteGroup _colourPalettes;

    [SerializeField] private Material _checkeredBackgroundMaterial;

    /// <summary>
    /// Callback is triggered when the colour palette is set
    /// </summary>
    public Action OnColourPaletteChange;

    public static ColourPaletteManager Singleton;

    public int ActivePaletteIndex = 0;

    private int _activePaletteIndex;

    void Awake()
    {
        if(Singleton != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(this.gameObject);

        LoadPalette(ActivePaletteIndex);
    }
    

    public Color GetColour(ColourTarget target, int colourIndex)
    {
        switch(target){
            
            case ColourTarget.COLOUR_PRIMARY: 

                colourIndex %= _colourPalettes.palettes[_activePaletteIndex].primaryBlockColours.Length;
                return _colourPalettes.palettes[_activePaletteIndex].primaryBlockColours[colourIndex];
            
            case ColourTarget.COLOUR_BACKGROUND_SILOUTTES:
                return _colourPalettes.palettes[_activePaletteIndex].backgroundColourSilouttes;
        }        
        
        return Color.black;
    }

    void Update()
    {
        if (ActivePaletteIndex != _activePaletteIndex)
        {
            LoadPalette(ActivePaletteIndex);
        }
    }

    void LoadPalette(int paletteIndex)
    {
        _activePaletteIndex = paletteIndex;

        // React to new palette on exisiting elements

        ColourPalette palette = _colourPalettes.palettes[_activePaletteIndex];

        OnColourPaletteChange?.Invoke();

        _checkeredBackgroundMaterial.SetColor("_ColorA", palette.backgroundColourA);
        _checkeredBackgroundMaterial.SetColor("_ColorB", palette.backgroundColourB);
    }



}
