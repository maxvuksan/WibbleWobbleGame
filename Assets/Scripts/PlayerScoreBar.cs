using UnityEngine;

public class PlayerScoreBar : MonoBehaviour
{
    
    [SerializeField] private int maxScore = 15;
    [SerializeField] private float fillSpeed = 4;
    [SerializeField] private SpriteRenderer _filledSpriteRenderer;
    [SerializeField] private Transform _toScale;

    private float _fillTracked = 0;
    private int _initalScore;
    private int _increasedScore;

    public void SetColour(Color colour)
    {
        _filledSpriteRenderer.color = colour;
    }
    public void SetInitalScore(int score)
    {
        _initalScore = score;
        _fillTracked = 0;
    }

    public void SetIncreasedScore(int score)
    {
        _increasedScore = score;
        _fillTracked = 0;
    }

    public void Update()
    {
        if(_fillTracked < 1)
        {
            _fillTracked += fillSpeed * Time.deltaTime;
        }
        else
        {
            _fillTracked = 1;
        }

        float t = Helpers.EaseInOutQuint(_fillTracked);
        float xScale = Mathf.Lerp((float)_initalScore / (float)maxScore, (float)_increasedScore / (float)maxScore, t);

        _toScale.localScale = new Vector3(xScale, 1, 1);
    }
}
