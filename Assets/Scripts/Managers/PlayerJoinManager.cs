using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinManager : MonoBehaviour
{
    private string _message = "";

    public void OnPlayerJoined(PlayerInput input)
    {
        _message = "Player joined: " + input.playerIndex + " " + input.currentControlScheme;
        Debug.Log(_message);
        
        //GetComponent<PlayerDataManager>().RegisterPlayer(input);
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        _message = "Player left: " + input.playerIndex + " " + input.currentControlScheme;
        Debug.Log(_message);
        
        //GetComponent<PlayerDataManager>().UnregisterPlayer(input);

    }

}
