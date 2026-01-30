using System.Collections.Generic;
using UnityEngine;

public class SpriteJiggleMultiState : MonoBehaviour
{

    [System.Serializable]
    public struct NamedState{
        
        public string name;
        public Sprite onSprite;
        public Sprite offSprite;
    }



    public SpriteRenderer target;
    public NamedState[] states;
    private Dictionary<string, SpriteJiggle.State> stateDictionary;
    [SerializeField] private string _currentState = "";


    private void Awake()
    {
        stateDictionary = new Dictionary<string, SpriteJiggle.State>();

        if(_currentState == "")
        {
            _currentState = states[0].name;
        }

        for (int i = 0; i < states.Length; i++)
        {
            var entry = states[i];

            SpriteJiggle.State state;
            state.onSprite = entry.onSprite;
            state.offSprite = entry.offSprite;

            // Prevent duplicate keys 

            if (!stateDictionary.ContainsKey(entry.name)){
                stateDictionary.Add(entry.name, state);
            }
        }
    }

    public void SetState(string newState)
    {
        this._currentState = newState;
        UpdateSprite();
    }

    private void UpdateSprite()
    {

        if(this._currentState == "")
        {
            return;
        }

        if (SpriteJiggleManager.Singleton.jiggleState)
        {
            target.sprite = stateDictionary[_currentState].onSprite;
        }
        else{
            target.sprite = stateDictionary[_currentState].offSprite;
        }
    }

    public void FixedUpdate() 
    {
        UpdateSprite();
    }
}
