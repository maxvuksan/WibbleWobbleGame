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
    public Color ropeColour;
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
    [SerializeField] private UnityEngine.UI.Image _dashedBorderRenderer;

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
    
    public ColourPalette GetColourPalette()
    {   
        return _colourPalettes.palettes[_activePaletteIndex];
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

    public void LoadPalette(int paletteIndex)
    {
        paletteIndex %= _colourPalettes.palettes.Length;

        ActivePaletteIndex = paletteIndex;
        _activePaletteIndex = paletteIndex;

        // React to new palette on exisiting elements

        ColourPalette palette = _colourPalettes.palettes[_activePaletteIndex];

        OnColourPaletteChange?.Invoke();

        if(_dashedBorderRenderer != null){
            _dashedBorderRenderer.color = _colourPalettes.palettes[_activePaletteIndex].backgroundColourSilouttes;
        }

        _checkeredBackgroundMaterial.SetColor("_ColorA", palette.backgroundColourA);
        _checkeredBackgroundMaterial.SetColor("_ColorB", palette.backgroundColourB);
    }



}
