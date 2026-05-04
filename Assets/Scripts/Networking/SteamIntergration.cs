using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;


public class SteamIntergration : MonoBehaviour
{

    private static SteamIntergration Singleton = null;


    void Awake() 
    {
        Helpers.CreateSingleton(ref Singleton, this);
    }


    static public void UnlockAchievement(string achievementId)
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }

        var ach = new Steamworks.Data.Achievement(achievementId);
        ach.Trigger();

        Debug.Log($"Achievement {achievementId} triggered");
    }

    static public void ClearAchievementStatus(string achievementId)
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }

        var ach = new Steamworks.Data.Achievement(achievementId);
        ach.Clear();
        Debug.Log($"Achievement {achievementId} cleared");
    }


    void Update()
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }

        SteamClient.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (!Configuration.Singleton.UseSteamTransport)
        {
            return;
        }

        SteamClient.Shutdown();
    }
    




}
