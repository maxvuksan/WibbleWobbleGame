using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class TrapToPlace : NetworkBehaviour
{

    [SerializeField] private TrapDictionary trapDictionary;
    [SerializeField] private float rotationSpeed;

    private GameObject _placementTrapGuideObject;
    private NetworkPlayerHeader _playerHeader;
    private ControllerInputHandler _inputHandler;


    private void Awake()
    {
        _inputHandler = FindFirstObjectByType<ControllerInputHandler>();
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
    }


    public void OnTrapIndexChanged(int oldValue, int newValue)
    {
        SetTrapType(newValue);
    }

    private void OnDisable()
    {
        if(_placementTrapGuideObject != null)
        {
            Destroy(_placementTrapGuideObject);
        }
    }


    /// <summary>
    /// Sets the trap type via index in the trap dictionary
    /// </summary>
    /// <param name="trapIndex">index of trap, setting this to -1 will remove the trap</param>
    public void SetTrapType(int trapIndex)
    {
        if(_placementTrapGuideObject != null)
        {
            Destroy(_placementTrapGuideObject);
        }

        if(trapIndex < 0)
        {
            return;
        }

        _placementTrapGuideObject = Instantiate(trapDictionary.traps[trapIndex].staticPrefab, transform);
        _placementTrapGuideObject.transform.localPosition = Vector3.zero;

        if (IsHost)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    public void Update()
    {

        if (!IsOwner)
        {
            return;
        }

        if (GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PlacingTrap ||
            GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_CreativeMode
        )
        {
            if (_inputHandler.Input.mainButtonIsPressed)
            {
                PlaceTrap();
            }
            if (_inputHandler.Input.rotateForwardButtonIsHeld)
            {
                RotateTrap(1);
            }
            if (_inputHandler.Input.rotateBackButtonIsHeld)
            {
                RotateTrap(-1);
            }             
        }

    }

    public void RotateTrap(float directionScaler)
    {
        transform.Rotate(new Vector3(0,0,rotationSpeed * Time.deltaTime * directionScaler));
    }

    public void PlaceTrap()
    {
        print("PLACE TRAP CALLED!");

        if(_playerHeader.SelectedTrap.Value < 0)
        {
            print("Cannot place trap. SelectedTrap is < 0");
            return;
        }
        else if(_playerHeader.PlacedTrap.Value)
        {
            print("Cannot place trap. PlacedTrap is == true");
            return;
        }
        else if(_placementTrapGuideObject == null){
            print("Cannot place trap. trap placement guide is null");
            return;
        }

        _placementTrapGuideObject.GetComponent<StaticTrap>().OnTrapPlace(transform.position);

        TrapPlacementArea.Singleton.AddTrapRpc(transform.position, _placementTrapGuideObject.transform.eulerAngles.z, _playerHeader.SelectedTrap.Value);


        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_CreativeMode)
        {
            Destroy(_placementTrapGuideObject);
            _playerHeader.SetSelectedTrapRpc(-1);
            _playerHeader.SetPlacedTrapRpc(true);
        }
        
        // TO DO: Temp commented...
        //PlayerDataManager.Singleton.PlayerData[_playerHeader.PlayerIndex.Value].mouseCursor.gameObject.SetActive(false);

    }

}
