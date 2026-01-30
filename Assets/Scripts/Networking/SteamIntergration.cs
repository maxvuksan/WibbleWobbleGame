using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;


public class SteamIntergration : MonoBehaviour
{

    private static SteamIntergration Singleton = null;


    private void Awake() 
    {
        if (Singleton != null)
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
    }


    static public void UnlockAchievement(string achievementId)
    {
        var ach = new Steamworks.Data.Achievement(achievementId);
        ach.Trigger();

        Debug.Log($"Achievement {achievementId} triggered");
    }

    static public void ClearAchievementStatus(string achievementId)
    {
        var ach = new Steamworks.Data.Achievement(achievementId);
        ach.Clear();
        Debug.Log($"Achievement {achievementId} cleared");
    }


    void Update()
    {
        SteamClient.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }
    




}
