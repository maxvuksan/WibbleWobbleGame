
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// A levels state, ready to serialize to a file
/// </summary>
public class LevelSaveData
{
    public LevelHeader Header; 
    public List<TrapPlacedData> Traps = new();
}

/// <summary>
/// Configurable data associated with a specific level, should be applied when a level is loaded
/// </summary>
[System.Serializable] 
public struct LevelHeader : INetworkSerializable
{
    public string Name;
    public int ColourPalette;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref ColourPalette);
    }
}

/// <summary>
/// Data that is computed and cached when a level loads
/// </summary>
[System.Serializable] 
public struct LevelRuntimeData : INetworkSerializable
{
    public float CameraXMin;
    public float CameraXMax;
    public int PlayerSpawnpointXHundreth;
    public int PlayerSpawnpointYHundreth;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CameraXMin);
        serializer.SerializeValue(ref CameraXMax);
        serializer.SerializeValue(ref PlayerSpawnpointXHundreth);
        serializer.SerializeValue(ref PlayerSpawnpointYHundreth);
    }
}