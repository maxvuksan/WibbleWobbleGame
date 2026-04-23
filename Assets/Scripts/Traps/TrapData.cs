using UnityEngine;


[System.Serializable]
public class TrapData
{
    public string name;
    public string soundOnPlace = "";
    public bool IsVisualOnly = false; // if is visual only, we do not need care about CustomTransform 
    [Header("Prefabs")]
    public GameObject behaviorPrefab;
}
