using UnityEngine;
using Volatile;

public class CustomSpringVisual : RopeVisual
{
    private CustomSpring spring;

    public override void Awake()
    {
        base.Awake();
        spring = GetComponent<CustomSpring>();

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void Update()
    {
        if(spring == null)
        {
            return;
        }

        if(spring.bodyA == null || spring.bodyB == null)
        {
            return;
        }
        SetPoint(0, spring.GetFloatStartAnchorPosition());
        SetPoint(1, spring.GetFloatEndAnchorPosition());
    }


}
