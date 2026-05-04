using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles configuration of a specific match
/// </summary>
public class MatchManager : NetworkBehaviour
{
    /// <summary>
    /// An array of level names that could be selected to play
    /// </summary>
    public string[] LevelPool;

    public static MatchManager Singleton;


    public void Awake()
    {
        Helpers.CreateSingleton(ref Singleton, this);        
    }

    public void ServerStartMatch()
    {

        if (!IsServer)
        {
            return;
        }

        string randomLevelName = LevelPool[Random.Range(0, LevelPool.Length)];
        
        bool result = LevelManager.Singleton.ServerLoadLevelFromFile(randomLevelName);

        if (result)
        {
            GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.PreviewLevel);
        }
        else // failed to load
        {
            // TODO: Handle error gracefully?
        }
    }
}
