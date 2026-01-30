using UnityEngine;

public class PlayerChip : MonoBehaviour
{

    [SerializeField] private GameObject _stateDead;
    [SerializeField] private GameObject _stateAlive;

    [SerializeField] private SpriteRenderer _spriteRendererAlive;
    [SerializeField] private SpriteRenderer _spriteRendererDead;

    public void SetIsAlive(bool isAlive)
    {
        if (isAlive)
        {
            _stateAlive.SetActive(true);
            _stateDead.SetActive(false);
        }
        else
        {
            _stateDead.SetActive(true);
            _stateAlive.SetActive(false);
        }
    }

    public void SetAliveColour(Color colour)
    {
        _spriteRendererAlive.color = colour;
    }
    public void SetDeadColour(Color colour)
    {
        _spriteRendererDead.color = colour;
    }
}
