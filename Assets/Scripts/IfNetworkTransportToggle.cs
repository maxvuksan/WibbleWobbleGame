using UnityEngine;

public class IfNetworkTransportToggle : MonoBehaviour
{
    public bool EnableIfUsingUnityTransport;

    void Awake()
    {
        if (EnableIfUsingUnityTransport == Configuration.Singleton.UseSteamTransport)
        {
            Destroy(this.gameObject);
        }
    }
}
