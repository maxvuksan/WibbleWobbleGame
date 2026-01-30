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
        if(target == ColourTarget.COLOUR_PRIMARY){

            colourIndex %= colourPalettes.palettes[_activePaletteIndex].primaryBlockColours.Length;
            return colourPalettes.palettes[_activePaletteIndex].primaryBlockColours[colourIndex];
        }
        
        return Color.black;
    }


    void LoadPalette(int paletteIndex)
    {
        _activePaletteIndex = paletteIndex;


        // React to new palette on exisiting elements

        ColourPalette palette = colourPalettes.palettes[_activePaletteIndex];

        backgroundRenderer.color = palette.backgroundColour;
    }



}
