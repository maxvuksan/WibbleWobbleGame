using FixMath.NET;
using UnityEngine;
using Volatile;

/// <summary>
/// A class to drive the Volatile physics simulation, this bridge the gap between Volatile and unity game objects
/// </summary>
public class CustomPhysicsSpace : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float _configWorldDamping = (float)VoltConfig.DEFAULT_DAMPING;

    private Fix64 _configWorldDampingFix64;


    public VoltWorld SimulationSpace
    {
        get => _simulationSpace;
    }
    
    private VoltWorld _simulationSpace;


    public static CustomPhysicsSpace Singleton;


    private void Awake() 
    {
        if(Singleton != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Singleton = this;

        InitFix64Constants();
        InitWorld();
    }


    /// <summary>
    /// Converts configuration variables to Fix64 variants
    /// </summary>
    private void InitFix64Constants()
    {
        _configWorldDampingFix64 = (Fix64)_configWorldDamping;
    }

    /// <summary>
    /// Initalize the physics simulation space
    /// </summary>
    private void InitWorld() 
    {
        _simulationSpace = new VoltWorld(_configWorldDampingFix64);
    }

    /// <summary>
    /// Steps the physics simulation forward 
    /// </summary>
    public void UpdateSimulation()
    {
        _simulationSpace.Update();
    }

    public void FixedUpdate()
    {
        UpdateSimulation();
    }


    


}
