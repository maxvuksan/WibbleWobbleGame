using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CreativeMode : NetworkBehaviour
{

    [SerializeField] private TrapDictionary _trapDictionary;
    [SerializeField] private float _uiSpacing = 1;
    [SerializeField] private Transform _uiParent;
    
    private int _trapIndex = 0;
    private Vector2 _uiTransformTarget = new Vector2(0,0);

    void Start()
    {
        for(int i = 0; i < _trapDictionary.traps.Length; i++)
        {
            GameObject trapUi = Instantiate(_trapDictionary.traps[i].staticPrefab, _uiParent);
            trapUi.transform.localPosition = new Vector3(i * _uiSpacing, 0, 0);     
        }
    }

    void Update()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        if(scrollDelta > 0)
        {
            _trapIndex++;
            UpdateSelectedTrap();
        }
        else if(scrollDelta < 0)
        {
            _trapIndex--;
            UpdateSelectedTrap();
        }

        _uiParent.localPosition = Vector3.MoveTowards(_uiParent.localPosition, _uiTransformTarget, Time.deltaTime * 10 * Vector3.Distance(_uiTransformTarget, _uiParent.localPosition));
    }

    private void UpdateSelectedTrap()
    {
        if(_trapIndex >= _trapDictionary.traps.Length)
        {
            _trapIndex = 0;
        }
        if(_trapIndex < 0)
        {
            _trapIndex = _trapDictionary.traps.Length - 1;
        }

        // move selected child up
        for(int i = 0; i < _uiParent.transform.childCount; i++)
        {
            float height = 0;
            if(i == _trapIndex)
            {
                height = 2;
            }

            _uiParent.GetChild(i).transform.localPosition = new Vector3(i * _uiSpacing, height, 0);    
        }

        List<NetworkPlayerHeader> players =  PlayerDataManager.Singleton.GetOwnedNetworkPlayerHeaders();
        for(int i = 0; i < players.Count; i++)
        {
            players[i].SetSelectedTrapRpc(_trapIndex);
        }

        _uiTransformTarget = new Vector3(_trapIndex * -_uiSpacing, 0, 0);
    }
}
