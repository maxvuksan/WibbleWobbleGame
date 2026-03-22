using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class Configuration : MonoBehaviour
{

    /// <summary>
    /// Is the game being debugged? Enabling this will display additional visuals and logs
    /// </summary>
    public bool DebugMode = false;

    /// <summary>
    /// Do logs get generated during runtime, this is only supported in debug mode
    /// </summary>
    public bool GenerateLogFilesInDebugMode = true;
    public bool LogsShouldIncludeClientInputs = true;
    public bool LogsShouldIncludePhysicsBodyState = true;
    public bool LogsShouldIgnoreStaticBodies = true;
    public bool LogsShouldIncludeCustomDataFromBodies = true;

    /// <summary>
    /// Determines if the Network Manager uses Facepunch steam transport, or the default Unity transport
    /// </summary>
    public bool UseSteamTransport = true;


    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] FacepunchTransport _transportFacepunch;
    [SerializeField] UnityTransport _transportUnity;
        
    public static Configuration Singleton;

    void Awake()
    {
        if(Singleton != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(this.gameObject);

        if(_networkManager == null)
        {
            Debug.LogWarning("Network manager is null, ignoring transport setup");
            return;
        }

        if (UseSteamTransport)
        {
            _networkManager.NetworkConfig.NetworkTransport = _transportFacepunch;
        }
        else
        {
            _networkManager.NetworkConfig.NetworkTransport = _transportUnity;
        }
    }


    void Start()
    {
        Application.targetFrameRate = 200;

        //TODO: V-sync is turned off becuase physics runs at 120 fps
        //QualitySettings.vSyncCount = 1;
    }
}
