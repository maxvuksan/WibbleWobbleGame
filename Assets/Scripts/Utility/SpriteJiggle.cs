using UnityEngine;

public class SpriteJiggle : MonoBehaviour
{
    
    public SpriteRenderer target;
    public State state;

    [System.Serializable]
    public struct State{
        
        public Sprite onSprite;
        public Sprite offSprite;
    }

    public void FixedUpdate() 
    {

        if (SpriteJiggleManager.Singleton.jiggleState)
        {
            target.sprite = state.onSprite;
        }
        else{
            target.sprite = state.offSprite;
        }

    }
}
