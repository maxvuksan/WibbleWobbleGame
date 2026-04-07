using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class TrapHeader : MonoBehaviour
{
    public string TrapName { get => trapName; }
    public bool IsUIElement = false;

    [Tooltip("This name should match the name of the trap within the trap dictionary")]
    [SerializeField] private string trapName;


    void Start()
    {
        if (IsUIElement)
        {
            WorldUIButton button = gameObject.AddComponent<WorldUIButton>();
            gameObject.layer = Helpers.Singleton.layerWorldUi;
            button.OnPressAction += OnPressUIButton;

            // turn off physics bodies for UI elements
            CustomPhysicsBody[] bodies = gameObject.GetComponentsInChildren<CustomPhysicsBody>();
            for(int i = 0; i < bodies.Length; i++)
            {
                bodies[i].enabled = false;
            }
        }        
    }

    void OnPressUIButton()
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        int trapIndex = TrapPlacementArea.Singleton.GetTrapIndexByName(trapName);
        PlayerDataManager.Singleton.PlayerData[(int)clientId].networkedPlayerHeader.SetSelectedTrapRpc(trapIndex);

        CollapsablePanel.CloseAllPanels();
    }
    
    /// <summary>
    /// Bolt a child trap to this block. makes the child move with this block
    /// </summary>
    public void AttachChildTrap(BoltHeader bolt)
    {
        bolt.transform.parent = this.transform;
    }


}
