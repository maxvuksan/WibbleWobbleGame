using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRoundOverScreen : NetworkBehaviour
{
    [SerializeField] private GameObject barPrefab;
    [SerializeField] private float spacing = 3;

    private List<PlayerScoreBar> _playerScoreBars;
    public static PlayerRoundOverScreen Singleton;

    public void Awake()
    {
        _playerScoreBars = new List<PlayerScoreBar>();
        Singleton = this;
    }

    public void Clear()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);    
            _playerScoreBars.Clear();
        }
    }

    public void Populate()
    {
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerData.Count; i++)
        {
            GameObject newBar = Instantiate(barPrefab, transform);
            newBar.transform.localPosition = new Vector3(0, spacing * i, 0);
            
            PlayerScoreBar playerScoreBar = newBar.GetComponent<PlayerScoreBar>();
            _playerScoreBars.Add(playerScoreBar);

            playerScoreBar.SetColour(PlayerDataManager.Singleton.PlayerDataPerIndex[i].colourBody);
            playerScoreBar.SetInitalScore(PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.Score.Value);
            playerScoreBar.SetIncreasedScore(PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.Score.Value);

            if (IsServer && 
                PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.HasWon.Value)
            {
                int newScore = PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.Score.Value + 1;
                
                PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetScoreRpc(newScore);
                PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetHasWonRpc(false);

                IncreaseScoreRpc(i, newScore);
            }
        }   
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void IncreaseScoreRpc(int playerIndex, int newScore)
    {
        _playerScoreBars[playerIndex].SetIncreasedScore(newScore);
    }


}
