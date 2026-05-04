using System;
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

        try
        {
            trapVisual = Instantiate(trapDictionary.traps[trapIndex].behaviorPrefab, wrldUiButtonObject.transform);
        }
        catch(InvalidOperationException exception)
        {
            Debug.Log("Exception caught on object with trapIndex: " + trapIndex + ", and name: " + gameObject.name);
            throw exception;
        }
                
        trapVisual.transform.localPosition = Vector3.zero;

        trapVisual.layer = uiLayer;
        for(int i = 0; i < trapVisual.transform.childCount; i++)
        {
            trapVisual.transform.GetChild(i).gameObject.layer = uiLayer;
        }
    } 

    public void PressTrap()
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        PlayerDataManager.Singleton.PlayerData[(int)clientId].NetworkedPlayerHeader.SetSelectedTrapRpc(_trapIndex);
    }

}
