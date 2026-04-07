using Unity.Netcode;
using UnityEngine;

public class ExitToWin : MonoBehaviour
{

    private CustomPhysicsBody _body;

    private void Start()
    {
        _body = GetComponent<CustomPhysicsBody>();
        _body.OnTrigger += OnTrigger;
    }

    private void OnDestroy()
    {   
        if(_body != null)
        {
            _body.OnTrigger -= OnTrigger;
        }
    }

    public void OnTrigger(CustomPhysicsBody collision)
    {
        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_Play)
        {
            return;
        }

        Player player = collision.GetComponent<Player>();

        // not our player...

        
        if(player != null)
        {
            print("Win reached");

            if (!player.IsOwner)
            {
                return;
            }

            player.ReachEnd();
            player.PlayerHeader.SetHasWonRpc(true);
        }
    }


}

