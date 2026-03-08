using Unity.Netcode;
using UnityEngine;
using Volatile;

public class Level : MonoBehaviour
{

    [SerializeField] private CustomTransform[] _playerSpawnpoints;

    public VoltVector2 GetSpawnpoint(int playerIndex)
    {
        return _playerSpawnpoints[playerIndex].GetPositionFix64();
    }
}
