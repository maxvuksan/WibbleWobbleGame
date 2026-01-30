using System;
using UnityEngine;

public class CameraFollowPlayers : MonoBehaviour
{
    
    [SerializeField] private Camera[] cameras;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform start;
    [SerializeField] private float smoothTime = 0.5f;
    private Vector3 _velocity;

    void Awake()
    {
        _velocity = new Vector3(0,0,0);
    }

    void LateUpdate()
    {
        Vector3 centrePoint = GetCentrePoint();

        centrePoint.y = transform.position.y;

        transform.position = Vector3.SmoothDamp(transform.position, centrePoint, ref _velocity, smoothTime);
    }

    Vector3 GetCentrePoint()
    {
        return new Vector3(0,0,0);
        /*

        if(PlayerDataManager.Singleton.PlayerCount == 0 || GameStateManager.Singleton.GetState() != GameStateManager.GameStateEnum.GameState_Play)
        {
            return new Vector3(0,0,0);
        }

        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bounds.Encapsulate(goal.transform.position);
        bounds.Encapsulate(start.transform.position);
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerData.Count; i++)
        {
            // skip dead players
            if (!PlayerDataManager.Singleton.PlayerData[i].alive)
            {
                continue;
            }

            bounds.Encapsulate(PlayerDataManager.Singleton.PlayerData[i].player.transform.position);
        }

        return bounds.center;
        */
    }

}
