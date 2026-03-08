using Unity.Netcode;
using UnityEngine;

public class UnityTransportMenuHandler : MonoBehaviour
{
    void Start()
    {
        #if UNITY_EDITOR
            if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor){

                print("Unity Transport: Starting Host");

                NetworkManager.Singleton.StartHost();
                Invoke("LoadNextScene", 4.0f);
            }
            else
            {
                print("Unity Transport: Starting Client");
                NetworkManager.Singleton.StartClient();
            }
        #endif
    }

    public void LoadNextScene()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
