using FixMath.NET;
using UnityEngine;
using Volatile;

public class Trap_Clamp : MonoBehaviour
{

    [SerializeField] private RopeVisual _ropeVisual;
    [SerializeField] private CustomPhysicsBody _hookAnchorBody;
    [SerializeField] private SpriteJiggleMultiState _hookEndJiggle;
    [SerializeField] private IntHundredth _hookSpeed;
    [SerializeField] private IntHundredth _hookSpeedIncrease;


    private TrapHeader _trapHeader;
    private bool _hookAttached;
    private VoltVector2 _hookOrigin;
    private Fix64 _hookYVelocity;
    private float _hookRotateOffset;
    private CustomSpring _spring;
    

    void Awake()
    {
        _spring = GetComponent<CustomSpring>();
        _trapHeader = GetComponent<TrapHeader>();

        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        _hookAttached = false;
    }
    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }

    void OnPhysicsTick()
    {
        if(_trapHeader.IsUIElement)
        {
            return;
        }

        _ropeVisual.SetPoint(0, transform.position);
        _ropeVisual.SetPoint(1, _hookEndJiggle.transform.position);

        if (_hookAttached)
        {
            if(_spring.bodyA == null || _spring.bodyB == null)
            {
                return;
            }

            _hookEndJiggle.transform.rotation =  Quaternion.Euler(new Vector3(0,0, _spring.bodyB.transform.eulerAngles.z - _hookRotateOffset));
            _hookEndJiggle.transform.position = new Vector3((float)_spring.GetEndAnchorPosition().x, (float)_spring.GetEndAnchorPosition().y);
            return;
        }

        if(CustomPhysics.Tick == 0)
        {
            _hookOrigin = GetComponent<CustomTransform>().GetPositionFix64();
            _hookYVelocity = Fix64.Zero;
        }

        CustomPhysicsRayResult hit = CustomPhysics.Raycast(_hookOrigin, new VoltVector2(Fix64.Zero, -Fix64.One), Fix64.Abs(_hookYVelocity));

        _hookYVelocity -= _hookSpeedIncrease * CustomPhysics.TimeBetweenTicks;
        _hookOrigin += new VoltVector2(Fix64.Zero, _hookYVelocity);

        _hookEndJiggle.transform.position = new Vector3(_hookEndJiggle.transform.position.x, (float)_hookOrigin.y);

        if (hit.Hit)
        {
            AudioManager.Singleton.Play("MetalClampLock");

            CustomPhysicsBody rb = hit.Body.gameObject.GetComponent<CustomPhysicsBody>();

            CustomSpring spring = GetComponent<CustomSpring>();

            spring.bodyA = _hookAnchorBody;
            spring.bodyB = rb;
            
            _hookRotateOffset = rb.transform.eulerAngles.z;

            VoltVector2 localPos = Helpers.TransformWorldPositionToLocalPosition(hit.HitPoint, rb.Position, rb.Angle);
            spring.bodyBAttachmentOffset = new(localPos.x, localPos.y);

            // make it move
            rb.Body.IsFixedPositionX = false;
            rb.Body.IsFixedPositionY = false;
            rb.Gravity = GameConstants.Singleton.GravityDefault;
            rb.Mass = GameConstants.Singleton.MassDefault;

            spring.CalculateRestLength();

            rb.BodyType = CustomBodyType.Dynamic;

            _hookAttached = true;
            _hookEndJiggle.SetState("Closed");
        }
    }
}
