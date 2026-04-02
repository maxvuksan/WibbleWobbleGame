using Unity.Netcode;
using UnityEngine;
using Volatile;

public class Level : MonoBehaviour
{

    [SerializeField] private CustomTransform[] _playerSpawnpoints;

    public IntHundredthVector2 GetSpawnpoint(int playerIndex)
    {
        return new IntHundredthVector2(_playerSpawnpoints[playerIndex].PositionXHundredth, _playerSpawnpoints[playerIndex].PositionYHundredth);
    }
}
