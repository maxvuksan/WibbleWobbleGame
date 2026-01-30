using UnityEngine;


[System.Serializable]
public class TrapData
{
    public string name;
    public string soundOnPlace = "";
    
    [Header("Prefabs")]
    public GameObject behaviorPrefab;
    public GameObject staticPrefab;

    [Header("Colours")]
    public ColourTarget colourTarget;
    public int colourTargetIndexOffset = 0;
}
