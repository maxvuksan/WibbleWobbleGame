using Unity.Netcode;
using UnityEngine;

public class TrapButton : NetworkBehaviour
{

    [SerializeField] private TrapDictionary trapDictionary;
    [SerializeField] private GameObject trapVisual;
    [SerializeField] private GameObject wrldUiButtonObject;
    public int uiLayer;
    private int _trapIndex;

    public void TriggerShow()
    {
        wrldUiButtonObject.SetActive(true);
    }
    public void TriggerHide()
    {
        wrldUiButtonObject.SetActive(false);
        SetTrapIndex(-1);
        Destroy(trapVisual);
    }

    //int i = Random.Range(0, trapDictionary.traps.Length);

    public void SetTrapIndex(int trapIndex)
    {
        this._trapIndex = trapIndex;

        if(trapVisual != null)
        {
            Destroy(trapVisual);
        }
        
        if(trapIndex == -1)
        {
            return;
        }

        trapVisual = Instantiate(trapDictionary.traps[trapIndex].staticPrefab, wrldUiButtonObject.transform);
        
        trapVisual.transform.localPosition = Vector3.zero;

        trapVisual.layer = uiLayer;
        for(int i = 0; i < trapVisual.transform.childCount; i++)
        {
            trapVisual.transform.GetChild(i).gameObject.layer = uiLayer;
        }
    } 

    public void PressTrap()
    {
        // iterate over each player, set the selected trap to this trap (what we pressed) and check if everyone has selected a trap...

        Debug.Log("PressButton" + NetworkManager.Singleton.LocalClientId);

        //for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        //{
        //    if(PlayerDataManager.Singleton.PlayerData[i].index == WorldUIButton.PlayerIndexWhoPressedButton)
        //    {
        //    }
        //}
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        PlayerDataManager.Singleton.PlayerData[(int)clientId].networkedPlayerHeader.SetSelectedTrapRpc(_trapIndex);
    }

}
