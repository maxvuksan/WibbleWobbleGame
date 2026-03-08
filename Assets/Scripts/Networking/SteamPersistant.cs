using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamPersistant : MonoBehaviour
{

    public static class LobbyKeys
    {
        public const string START_GAME = "start_game";
    }


    public enum LobbyAuthority
    {
        NOT_CONNECTED,
        HOST,
        MEMBER,
    }

    public class SteamContext{
        
        public Lobby lobby;
        public LobbyAuthority lobbyAuthority = LobbyAuthority.NOT_CONNECTED;
    }

    public static SteamContext Context;
    private static SteamPersistant _Singleton;





    private void Awake() 
    {
        if (Configuration.Singleton.UseSteamTransport)
        {
            if (_Singleton != null)
            {
                Destroy(this);
                return;
            }

            _Singleton = this;
            Context = new SteamContext();

            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }
        
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
    }
    private void OnDestroy()
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }

        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
    }

    /// <summary>
    /// Steam Callbacks _____________________________________________________________
    /// </summary>

    private void OnLobbyDataChanged(Lobby lobby)
    {
        if(Context.lobby.Id != lobby.Id)
        {
            return;
        }

        var startValue = lobby.GetData(LobbyKeys.START_GAME);

        if(startValue != "1")
        {
            return;    
        }

        LobbyData_StartGame();
    }

    private void LobbyData_StartGame()
    {
        // Start game already called...
        //if (NetworkManager.Singleton.IsListening)
        //{
        //    return;
        //}
        
        if(Context.lobbyAuthority == LobbyAuthority.HOST)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);

        }
    }

}
