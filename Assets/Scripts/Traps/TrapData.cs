using UnityEngine;


[System.Serializable]
public class TrapData
{
    public string name;
    public string soundOnPlace = "";
    
    [Header("Prefabs")]
    public GameObject behaviorPrefab;
}
