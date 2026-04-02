using UnityEngine;

public class CustomSpringVisual : RopeVisual
{
    private CustomSpring spring;

    public override void Awake()
    {
        base.Awake();
        spring = GetComponent<CustomSpring>();
    }

    void Update()
    {
        SetPoint(0, spring.GetStartAnchorPosition());
        SetPoint(1, spring.GetEndAnchorPosition());
    }


}
