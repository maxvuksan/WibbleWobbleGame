using UnityEngine;

public class Configuration : MonoBehaviour
{

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
        QualitySettings.vSyncCount = 1;
    }
}
