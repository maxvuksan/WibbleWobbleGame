
using System.Collections.Generic;

/// <summary>
/// A levels state, ready to serialize to a file
/// </summary>
public class LevelSaveData
{
    public List<TrapPlacedData> Traps = new();
}