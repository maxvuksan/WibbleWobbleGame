using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShakyText : MonoBehaviour
{

    [SerializeField] private TextMeshPro _letterPrefab;
    [SerializeField] private float _spacing = 1f;
    [SerializeField] private float _driftStrength = 1;
    [SerializeField] private float _driftSpeed = 0.5f;
    [Range(0, 1)]
    [SerializeField] private float _letterDriftOffset = 0.2f;

    private List<GameObject> _letters;
    private List<Vector2> _letterOrigins;

    void Awake()
    {
        _letters = new List<GameObject>();
        _letterOrigins = new List<Vector2>();
    }

    /// <summary>
    /// Adds a string of text to the sequence, this is intended for adding characters 
    /// </summary>
    /// <param name="letter">The characters we wish to add</param>
    public void AddCharacter(string letter)
    {
        GameObject newObj = Instantiate(_letterPrefab.gameObject, transform);
        newObj.GetComponent<TextMeshPro>().text = letter;

        _letters.Add(newObj);

        CalculateLetterSpacing();
    }

    /// <summary>
    /// Clears all the letters added, this destroys each spawned letter prefab
    /// </summary>
    public void ClearLetters()
    {
        for(int i = 0; i < _letters.Count; i++)
        {
            Destroy(_letters[i].gameObject);
        }

        _letters.Clear();
        _letterOrigins.Clear();
    }

    private void CalculateLetterSpacing()
    {
        _letterOrigins.Clear();
        
        float totalWidth = (_letters.Count - 1) * _spacing;
        float startX = -totalWidth / 2f;
        
        for(int i = 0; i < _letters.Count; i++)
        {
            Vector2 originPos = new Vector2(startX + (i * _spacing), 0f);
            _letterOrigins.Add(originPos);
        }
    }

    void Update()
    {
        ApplyLetterPositions();
    }

    /// <summary>
    /// Applies the letter positions with drift effect
    /// </summary>
    private void ApplyLetterPositions()
    {
        for(int i = 0; i < _letters.Count; i++)
        {
            float offsetSeed = i * _letterDriftOffset;
            float noiseX = Mathf.PerlinNoise(Time.time * _driftSpeed + offsetSeed, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, Time.time * _driftSpeed + offsetSeed) * 2f - 1f;
            
            Vector2 driftOffset = new Vector2(noiseX, noiseY) * _driftStrength;

            // Apply origin position + drift
            Vector3 newPos = _letters[i].transform.localPosition;
            newPos.x = _letterOrigins[i].x + driftOffset.x;
            newPos.y = _letterOrigins[i].y + driftOffset.y;
            _letters[i].transform.localPosition = newPos;
        }
    }
}
