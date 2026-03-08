using System;
using UnityEngine;
using UnityEngine.Events;

public class TrapHeader : MonoBehaviour
{
    public string TrapName { get => trapName; }

    [Tooltip("This name should match the name of the trap within the trap dictionary")]
    [SerializeField] private string trapName;


    void Awake()
    {
        
    }

    /// <summary>
    /// Is called when this trap is placed
    /// </summary>
    // virtual public void OnTrapPlace()
    // {
        
    // }
    
    /// <summary>
    /// Bolt a child trap to this block. makes the child move with this block
    /// </summary>
    public void AttachChildTrap(BoltHeader bolt)
    {
        bolt.transform.parent = this.transform;
    }


}
