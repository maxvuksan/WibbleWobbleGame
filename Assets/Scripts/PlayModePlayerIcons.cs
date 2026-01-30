using System.Collections.Generic;
using UnityEngine;

public class PlayModePlayerIcons : MonoBehaviour
{

    [SerializeField] private GameObject _playerChipPrefab;
    [SerializeField] private float _chipSpacing = 3;

    private List<PlayerChip> _playerChips;

    private void CleanupIcons() {
        
        PlayerDataManager.Singleton.OnPlayerDeath -= OnPlayerDeath;

        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void RecomputeIcons()
    {
        CleanupIcons();

        PlayerDataManager.Singleton.OnPlayerDeath += OnPlayerDeath;

        _playerChips = new List<PlayerChip>();

        // spawn in chips
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            GameObject newChip = Instantiate(_playerChipPrefab, transform);     
            newChip.transform.localPosition = new Vector3(0, -_chipSpacing * i, 0);

            newChip.GetComponent<PlayerChip>().SetAliveColour(PlayerDataManager.Singleton.PlayerDataPerIndex[i].colourBody);
            _playerChips.Add(newChip.GetComponent<PlayerChip>());
        }        
    }

    private void OnPlayerDeath(ulong playerIndex)
    {
        _playerChips[(int)playerIndex].SetIsAlive(false);
    }
}
