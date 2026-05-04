using System;
using System.Collections.Generic;
using FixMath.NET;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Volatile;


public struct AttachedTrap
{
    public TrapHeader Header;
    public CustomPhysicsBody Body;
    public CustomPhysicsBody ParentBody;
    public VoltVector2 LocalOffset; // Child position relative to parent center in parent local space
    public Fix64 RelativeRotation;  // Difference in rotation between child and parent
}

public class TrapHeader : MonoBehaviour
{
    public string TrapName { get => trapName; }
    public bool IsUIElement = false;
    public List<AttachedTrap> attachedTraps;

    [Tooltip("This name should match the name of the trap within the trap dictionary")]
    [SerializeField] private string trapName;


    void Awake()
    {
        attachedTraps = new List<AttachedTrap>();
        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;
    }

    public virtual void OnDestroy()
    {
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;
    }

    public virtual void Start()
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
        PlayerDataManager.Singleton.PlayerData[(int)clientId].NetworkedPlayerHeader.SetSelectedTrapRpc(trapIndex);

        CollapsablePanel.CloseAllPanels();
    }
    
    /// <summary>
    /// Attach/parent this child trap to this block. makes the child move with this block
    /// </summary>
    public void AttachChildBody(TrapHeader trap, CustomPhysicsBody childBody, CustomPhysicsBody parentBody){

        // Calculate the vector from Parent to Child in World Space
        VoltVector2 worldOffset = childBody.Position - parentBody.Position;

        // Rotate that vector into the Parent's Local Space
        VoltVector2 localOffset = Helpers.RotatePosition(worldOffset, -parentBody.Body.Angle);

        // Store the rotation difference
        Fix64 relativeRot = childBody.Body.Angle - parentBody.Body.Angle;

        var entry = new AttachedTrap()
        {
            Header = trap,
            Body = childBody,
            ParentBody = parentBody,
            LocalOffset = localOffset,
            RelativeRotation = relativeRot
        };

        // Disable physics forces on the child so it follows the parent smoothly
        // childBody.Body.IsKinematic = true; 

        attachedTraps.Add(entry);
    }

    public void OnPostPhysicsTick()
    {
        for(int i = 0; i < attachedTraps.Count; i++)
        {
            var child = attachedTraps[i];
            Fix64 parentAngle = child.ParentBody.Body.Angle; 
            
            // Calculate new world position based on parent's current state
            VoltVector2 rotatedOffset = Helpers.RotatePosition(child.LocalOffset, parentAngle);
            VoltVector2 newWorldPos = child.ParentBody.Position + rotatedOffset;
            Fix64 newWorldAngle = parentAngle + child.RelativeRotation;

            // Apply directly to the body
            child.Body.Body.Set(newWorldPos, newWorldAngle);
        }
    }


}
