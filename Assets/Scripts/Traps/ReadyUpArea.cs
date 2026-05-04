using FixMath.NET;
using Unity.Netcode;
using UnityEngine;
using Volatile;

/// <summary>
/// The area all players must stand in to start the match
/// </summary>
public class ReadyUpArea : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] Color NotReadyColour;
    [SerializeField] Color ReadyColour; 
    [SerializeField] private float _requiredReadyTimeSeconds = 3.0f; 
    private float _readyTimeTracked = 0.0f;



    void Awake()
    {
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }

    void OnPhysicsTick()
    {
        var result = CustomPhysics.OverlapRect(
            new VoltVector2((Fix64)_spriteRenderer.bounds.center.x, (Fix64)_spriteRenderer.bounds.center.y), 
            new VoltVector2((Fix64)_spriteRenderer.bounds.extents.x, (Fix64)_spriteRenderer.bounds.extents.y),
            true);
    
        int readyCount = 0;
        foreach(var body in result.Bodies)
        {
            if (body.GetComponent<Player>())
            {
                readyCount++;                
            }
        }

        if(readyCount == PlayerDataManager.Singleton.PlayerCount)
        {
            _spriteRenderer.color = ReadyColour;
            _readyTimeTracked += (float)CustomPhysics.TimeBetweenTicks;

            if(_readyTimeTracked > _requiredReadyTimeSeconds)
            {
                MatchManager.Singleton.ServerStartMatch();
                _readyTimeTracked = 0;
            }
        }
        else
        {
            _readyTimeTracked = 0;
            _spriteRenderer.color = NotReadyColour;
        }
    }
}
