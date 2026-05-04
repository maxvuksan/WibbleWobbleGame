using Unity.VisualScripting;
using UnityEngine;

public class LevelPreviewManager : MonoBehaviour
{

    public static LevelPreviewManager Singleton;

    [SerializeField] private float _timeBetweenLetters;
    private float _timeBetweenLettersTracked;
    private int _letterIndex;
    private string _textToType;
    private ShakyText _shakyText;
    private bool _doType;

    void Awake()
    {
        Singleton = this;
        _doType = false;
    }

    void OnEnable()
    {
        _shakyText = GetComponent<ShakyText>();
    }
    void OnDisable()
    {
        _letterIndex = 0;
        _shakyText.ClearLetters();
        _doType = false;
    }

    /// <summary>
    /// Begin the animation for typing of the level preview text
    /// </summary>
    /// <param name="levelName">The text we wish to type out</param>
    public void TypeOutLevelText(string levelName)
    {
        _letterIndex = 0;
        _shakyText.ClearLetters();
        
        _textToType = levelName;
        _doType = true;
        _timeBetweenLettersTracked = 0;
    }

    /// <summary>
    /// Adds the next letter (determined by the current index) from the textToType string
    /// </summary>
    private void AddLetterFromTextToType()
    {
        _shakyText.AddCharacter("" + _textToType[_letterIndex]);
        AudioManager.Singleton.Play("KeyboardPress");
    }

    public void Update()
    {
        if (_doType)
        {
            _timeBetweenLettersTracked += Time.deltaTime;
            
            if(_timeBetweenLettersTracked > _timeBetweenLetters)
            {
                AddLetterFromTextToType();
                _letterIndex++;
                _timeBetweenLettersTracked = 0;
            }
        
            if(_letterIndex == _textToType.Length)
            {
                _doType = false;
            }
        }
    }
}
