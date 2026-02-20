using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Which input direction is the player pressing?, represented as a byte
/// </summary>
public enum PlayerMoveInput : byte {
    None,
    LeftPressed,
    RightPressed,
}

/// <summary>
/// Has the player pressed jump?, represented as a byte
/// </summary>
public enum PlayerJumpInput : byte {
    None,
    JumpPressed
}

/// <summary>
/// A data representation of the a specific players inputs
/// </summary>
[System.Serializable]
public class PlayerClientInputs : INetworkSerializable
{
    public PlayerMoveInput InputMoveDirection;
    public PlayerJumpInput InputJump;

    public PlayerClientInputs()
    {
        InputJump = PlayerJumpInput.None;
        InputMoveDirection = PlayerMoveInput.None;
    }

    public PlayerClientInputs(PlayerClientInputs other)
    {
        InputMoveDirection = other.InputMoveDirection;
        InputJump = other.InputJump;
    }

    /// <summary>
    /// Required for Netcode serialization
    /// </summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InputMoveDirection);
        serializer.SerializeValue(ref InputJump);
    }

    /// <returns>The players horizontal input convert to an integer representation</returns>
    public int GetMoveDirection()
    {
        if(InputMoveDirection == PlayerMoveInput.LeftPressed)
        {
            return -1;
        }
        else if(InputMoveDirection == PlayerMoveInput.RightPressed)
        {
            return 1;
        }
        return 0;
    }

}
