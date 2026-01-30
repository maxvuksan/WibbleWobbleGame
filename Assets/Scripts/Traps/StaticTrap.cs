using UnityEngine;

public class StaticTrap : MonoBehaviour
{

    public string TrapName { get => trapName; }

    [Tooltip("This name should match the name of the trap within the trap dictionary")]
    [SerializeField] private string trapName;


    public void Awake()
    {
        TrapPlacementArea.Singleton.ApplyColourPaletteToTrap(trapName, this.gameObject);
    }

    /// <summary>
    /// Removes this trap from the placement area (using this static instance as reference for what to remove)
    /// </summary>
    public void RemoveTrap()
    {
        FindFirstObjectByType<TrapPlacementArea>().RemoveTrapThroughStaticInstance(this.gameObject);
    }

    /// <summary>
    /// Is called when a trap is placed, can be overridden to add trap specific behaviour
    /// </summary>
    public virtual void OnTrapPlace(Vector2 trapPlacePosition)
    {
        
    }
}
