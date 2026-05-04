using UnityEngine;

public class Trap_CrumbleBlock : MonoBehaviour
{

    private CustomPhysicsBody _body;
    private SpriteJiggleMultiState _spriteAnimator;

    private bool _triggered;
    private int _ticksPassed;

    void Start()
    {
        _ticksPassed = 0;
        _triggered = false;

        CustomPhysics.OnPhysicsTick += OnPhysicsTick;

        _spriteAnimator = GetComponent<SpriteJiggleMultiState>();
        _body = GetComponent<CustomPhysicsBody>();
        _body.OnTrigger += OnTrigger;
    }
    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        if(_body != null)
        {
            _body.OnTrigger -= OnTrigger;
        }
    }

    void OnTrigger(CustomPhysicsBody otherBody)
    {
        // only players can trigger stone blocks
        if(otherBody.GetComponent<Player>() == null)
        {
            return;
        }

        _triggered = true;
    }

    void OnPhysicsTick()
    {
        if (!_triggered)
        {
            return;
        }

        _ticksPassed++;

        if(_ticksPassed > CustomPhysics.TicksPerSecond * 2)
        {
            _body.Body.IsEnabled = false;
            _spriteAnimator.target.enabled = false;
            _triggered = false;
        }
        else if(_ticksPassed > CustomPhysics.TicksPerSecond)
        {
            _spriteAnimator.SetState("BigCrack");
        }
        else if(_ticksPassed > CustomPhysics.TicksPerSecond / 2)
        {
            _spriteAnimator.SetState("SmallCrack");
        }
        
        if(CustomPhysics.Tick % GameConstants.Singleton.CrumbleBlockShakeDelay == 0)
        {
            float offsetX = Random.Range(-GameConstants.Singleton.CrumbleBlockShakeStrength, GameConstants.Singleton.CrumbleBlockShakeStrength);
            float offsetY = Random.Range(-GameConstants.Singleton.CrumbleBlockShakeStrength, GameConstants.Singleton.CrumbleBlockShakeStrength);
                
            _spriteAnimator.target.transform.localPosition = new Vector3(offsetX, offsetY, 0);   
        }
    }

}
