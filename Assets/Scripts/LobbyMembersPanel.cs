using UnityEngine;

public class LobbyMembersPanel : MonoBehaviour
{
    [SerializeField] private GameObject _playerEntry;
    [SerializeField] private Transform _playerEntryParent;
    [SerializeField] private float _verticalSpacing;

    public void Start()
    {
        PlayerDataManager.Singleton.OnRegisterPlayer += ConstructPlayerEntries;
        ConstructPlayerEntries();
    }

    private void OnDestroy() 
    {
        PlayerDataManager.Singleton.OnRegisterPlayer -= ConstructPlayerEntries;
    }

    public void ConstructPlayerEntries()
    {   
        print("ConstructPlayerEntries");

        ClearEntries();
        
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            GameObject newEntry = Instantiate(_playerEntry, _playerEntryParent);
            newEntry.transform.localPosition = new Vector3(0, _verticalSpacing * i);

            var entry = newEntry.GetComponent<LobbyMembersPlayerEntry>();

            // TODO: Each player should have a stored name variable, this is for steam lobbies the steam name, otherwise Player1, player 2 etc...
            entry.SetName("This is a test");
            entry.SetPlayerColour(PlayerDataManager.Singleton.PlayerDataPerIndex[PlayerDataManager.Singleton.PlayerData[i].index].colourBody);
       
            print("ConstructPlayerEntries, " + i);
       
        }
    }

    private void ClearEntries()
    {
        for (int i = _playerEntryParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_playerEntryParent.GetChild(i).gameObject);
        }
    }
}
