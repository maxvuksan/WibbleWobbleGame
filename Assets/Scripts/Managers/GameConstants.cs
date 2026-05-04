using Unity.VisualScripting;
using UnityEngine;

public class GameConstants : MonoBehaviour
{
    [Header("Physics")]
    public IntHundredth GravityDefault = 30;
    public IntHundredth MassDefault = 9;

    [Header("Traps")]
    public float CrumbleBlockShakeStrength = 0.1f;
    public int CrumbleBlockShakeDelay = 10;

    public static GameConstants Singleton; 

    void Awake()
    {
        Helpers.CreateSingleton(ref Singleton, this);
    }
}
