using UnityEngine;

public class Configuration : MonoBehaviour
{

    public bool DebugMode = false;
    
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
    }


    void Start()
    {
        Application.targetFrameRate = 200;

        //TODO: V-sync is turned off becuase physics runs at 120 fps
        //QualitySettings.vSyncCount = 1;
    }
}
