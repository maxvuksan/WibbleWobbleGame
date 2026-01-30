using System;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyUiManager : MonoBehaviour
{

    [SerializeField] private GameObject[] enableWhenLobbyHost;
    [SerializeField] private GameObject[] enableWhenLobbyMember;
    [SerializeField] private GameObject[] enableWhenNoLobby;

    [SerializeField] private Transform lobbyMemberTransformParent;
    [SerializeField] private GameObject lobbyMemberTextPrefab;

    [SerializeField] private TMP_InputField lobbyIdInput;

    void Start()
    {
        RefreshAllLobbyUI();

        // subscribing for steam callbacks...
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

        SteamFriends.OnGameLobbyJoinRequested += OnGameInviteAccepted;
        SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;
    }
    void OnDestroy()
    {
        // unsubscribing for steam callbacks....
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
    }


    public void Button_JoinFriend()
    {
        SteamFriends.OpenOverlay("friends");
    }

    public async void Button_JoinId()
    {
        string idString = lobbyIdInput.text.Trim().Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "");

        Debug.Log(idString);

        if(ulong.TryParse(idString, out ulong lobbyId))
        {
            Debug.Log("Joined lobby with id: " + idString + " Button_JoinId()");
            await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        }

    }

    public void Button_InviteFriends()
    {
        if (SteamPersistant.Context.lobbyAuthority == SteamPersistant.LobbyAuthority.NOT_CONNECTED){
            Debug.Log("We are not connected to a lobby, return early. Button_InviteFriends()");
            return;
        }

        Debug.Log("Opened game invite overlay. Button_InviteFriends()");

        SteamFriends.OpenGameInviteOverlay(SteamPersistant.Context.lobby.Id);
    }

    public void Button_Leave()
    {
        if (SteamPersistant.Context.lobbyAuthority == SteamPersistant.LobbyAuthority.NOT_CONNECTED){
            Debug.Log("We are not connected to a lobby, return early. Button_Leave()");
            return;
        }

        Debug.Log("Leaving lobby. Button_Leave");

        SteamPersistant.Context.lobby.Leave();
        SteamPersistant.Context.lobbyAuthority = SteamPersistant.LobbyAuthority.NOT_CONNECTED;
        
        RefreshAllLobbyUI();
    }
    
    public void Button_Start()
    {
        if(SteamPersistant.Context.lobbyAuthority == SteamPersistant.LobbyAuthority.NOT_CONNECTED){
            return;
        }

        SteamPersistant.Context.lobby.SetData(SteamPersistant.LobbyKeys.START_GAME, "1");
    }

    public async void Button_Host()
    {
        await SteamMatchmaking.CreateLobbyAsync(8);
    }




    /// <summary>
    /// Recomputes which game objects should be active depending on the lobby authority. Additionally refreshes the lobbies member list if connected
    /// </summary>
    private void RefreshAllLobbyUI()
    {
        // enables the appropriate game objects for the players lobby authority...

        switch (SteamPersistant.Context.lobbyAuthority)
        {
            case SteamPersistant.LobbyAuthority.NOT_CONNECTED:

                Helpers.SetActiveGameObjectArray(enableWhenLobbyHost, false);
                Helpers.SetActiveGameObjectArray(enableWhenLobbyMember, false);

                Helpers.SetActiveGameObjectArray(enableWhenNoLobby, true);
                
                break;

            case SteamPersistant.LobbyAuthority.HOST:

                Helpers.SetActiveGameObjectArray(enableWhenNoLobby, false);
                Helpers.SetActiveGameObjectArray(enableWhenLobbyMember, false);
                
                Helpers.SetActiveGameObjectArray(enableWhenLobbyHost, true);

                break;

            case SteamPersistant.LobbyAuthority.MEMBER:

                Helpers.SetActiveGameObjectArray(enableWhenNoLobby, false);
                Helpers.SetActiveGameObjectArray(enableWhenLobbyHost, false);
                
                Helpers.SetActiveGameObjectArray(enableWhenLobbyMember, true);

                break;
        }

        RefreshLobbyMemberListUI();
    }

    /// <summary>
    /// Refreshes the connected lobbies member list, if we are not in a lobby this will simply clear the list.
    /// </summary>
    private void RefreshLobbyMemberListUI()
    {

        // destroy all lobby member text elements

        for(int i = 0; i < lobbyMemberTransformParent.childCount; i++)
        {
            Destroy(lobbyMemberTransformParent.GetChild(i).gameObject);
        }


        if(SteamPersistant.Context.lobbyAuthority == SteamPersistant.LobbyAuthority.NOT_CONNECTED)
        {
            return;
        }

        lobbyIdInput.text = SteamPersistant.Context.lobby.Id.ToString();
        Debug.Log("Lobby Id is : " + lobbyIdInput.text);
        // spawn updated member list

        foreach (var member in SteamPersistant.Context.lobby.Members)
        {
            var go = Instantiate(lobbyMemberTextPrefab, lobbyMemberTransformParent);
            var text = go.GetComponent<TextMeshProUGUI>();

            text.text = member.Name;

            // mark host with Host label
            if (member.Id == SteamPersistant.Context.lobby.Owner.Id){
                text.text += " (Host)";
            }
        }
    }


    /// <summary>
    /// Steam Callbacks _____________________________________________________________
    /// </summary>

    private async void OnGameInviteAccepted(Lobby lobby, SteamId steamId)
    {
        Debug.Log("OnGameInviteAccepted() called, trying to join lobby");
        await lobby.Join();
    }

    private async void OnGameRichPresenceJoinRequested(Friend friend, string s)
    {
        Debug.Log("OnGameRichPresenceJoinRequested() called, trying to join lobby");
        
        if(ulong.TryParse(s, out ulong sessionId))
        {
            Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(sessionId);

            if(lobby == null)
            {
                Debug.Log("The requested lobby is null, OnGameRichPresenceJoinRequested()");
            }
        }
        else
        {
            Debug.Log("Malformed lobby session id, OnGameRichPresenceJoinRequested()");
        }
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        Debug.Log("OnLobbyCreated()");

        lobby.SetJoinable(true);
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log("OnLobbyEntered()");

        SteamPersistant.Context.lobby = lobby;
        
        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            SteamPersistant.Context.lobbyAuthority = SteamPersistant.LobbyAuthority.HOST;
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            SteamPersistant.Context.lobbyAuthority = SteamPersistant.LobbyAuthority.MEMBER;
            NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
            NetworkManager.Singleton.StartClient();
        }

        RefreshAllLobbyUI();
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friendThatLeft)
    {
        Debug.Log(friendThatLeft.Name + " has left the lobby. OnLobbyMemberLeave()");
        
        // Lobby is not nullable... so instead we trust lobby authority
        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            SteamPersistant.Context.lobbyAuthority = SteamPersistant.LobbyAuthority.HOST;
        }
        else
        {
            SteamPersistant.Context.lobbyAuthority = SteamPersistant.LobbyAuthority.MEMBER;
        }

        RefreshAllLobbyUI();
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friendThatLeft)
    {
        Debug.Log(friendThatLeft.Name + " has joined the lobby. OnLobbyMemberJoined()");

        RefreshAllLobbyUI();
    }
    



}
