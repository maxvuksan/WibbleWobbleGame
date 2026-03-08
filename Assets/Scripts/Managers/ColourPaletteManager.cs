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
    public Color backgroundColour;
}

/// <summary>
/// ColourPaletteManager is responsible for changing the colours of traps, background and ui elements.
/// </summary>
public class ColourPaletteManager : MonoBehaviour
{
    [SerializeField] private ColourPaletteGroup colourPalettes;


    // ______________________________

    [SerializeField] SpriteRenderer backgroundRenderer;

    // ______________________________

    /// <summary>
    /// Callback is triggered when the colour palette is set
    /// </summary>
    public Action OnColourPaletteChange;

    public static ColourPaletteManager Singleton;


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

        LoadPalette(0);
    }
    

    public Color GetColour(ColourTarget target, int colourIndex)
    {
        switch(target){
            
            case ColourTarget.COLOUR_PRIMARY: 

                colourIndex %= colourPalettes.palettes[_activePaletteIndex].primaryBlockColours.Length;
                return colourPalettes.palettes[_activePaletteIndex].primaryBlockColours[colourIndex];
            
            case ColourTarget.COLOUR_BACKGROUND_SILOUTTES:
                return colourPalettes.palettes[_activePaletteIndex].backgroundColourSilouttes;
        }        
        
        return Color.black;
    }


    void LoadPalette(int paletteIndex)
    {
        _activePaletteIndex = paletteIndex;


        // React to new palette on exisiting elements

        ColourPalette palette = colourPalettes.palettes[_activePaletteIndex];

        backgroundRenderer.color = palette.backgroundColour;

        OnColourPaletteChange?.Invoke();
    }



}
