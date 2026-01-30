using Unity.Netcode;
using UnityEngine;

public class ExitToWin : NetworkBehaviour
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_Play)
        {
            return;
        }

        Player player = collision.GetComponent<Player>();

        // not our player...
        if (!player.IsOwner)
        {
            return;
        }

        if(player != null)
        {
            player.ReachEnd();
            player.PlayerHeader.SetHasWonRpc(true);
        }
    }


}

