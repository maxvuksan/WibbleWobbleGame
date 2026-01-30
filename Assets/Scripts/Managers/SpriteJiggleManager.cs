using System;
using UnityEngine;

public class SpriteJiggleManager : MonoBehaviour
{
    
    public static SpriteJiggleManager Singleton;

    [SerializeField] private int jiggleFrames = 12;
    [HideInInspector] private int jiggleSpeedTracked = 0;
    [HideInInspector] public bool jiggleState = false;


    public void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public void FixedUpdate()
    {
        jiggleSpeedTracked += 1;

        if(jiggleSpeedTracked > jiggleFrames)
        {
            jiggleState = !jiggleState;
            jiggleSpeedTracked = 0;
        }
    }
}
