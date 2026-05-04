using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;

public class Trap_StatusField : MonoBehaviour
{
    private CustomPhysicsBody _body;

    private Dictionary<ulong, Fix64> _originalGravity = new();
    private HashSet<ulong> _currentBodies = new();
    private HashSet<ulong> _tempBodiesToRevert = new();

    public void Start() 
    {
        _body = GetComponent<CustomPhysicsBody>();
        _body.OnTrigger += OnTrigger;

        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;
    }

    public void OnDestroy()
    {
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;

        if(_body != null)
        {
            _body.OnTrigger -= OnTrigger;
        }
    }

    private void OnTrigger(CustomPhysicsBody otherBody)
    {
        if (otherBody.BodyType == CustomBodyType.Static){
            return;
        }

        ulong id = otherBody.Body.EntityId;

        if (!_originalGravity.ContainsKey(id)){
            _originalGravity[id] = otherBody.Gravity;
        }

        _currentBodies.Add(id);

        otherBody.Gravity = Fix64.Zero;
    }

    private void OnPostPhysicsTick()
    {
        _tempBodiesToRevert.Clear();

        foreach (var id in _originalGravity.Keys)
        {
            if (!_currentBodies.Contains(id))
            {
                var body = CustomPhysicsSpace.Singleton.GetBody(id);

                if (body != null)
                {
                    body.Gravity = _originalGravity[id];
                }

                _tempBodiesToRevert.Add(id);
            }
        }

        foreach (var id in _tempBodiesToRevert)
        {
            _originalGravity.Remove(id);
        }

        _currentBodies.Clear();
    }

}
