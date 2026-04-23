using UnityEngine;

public class PlayerDeathHead : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _deathHead;

    public void SetColour(Color colour)
    {
        _deathHead.color = colour;
    }

}
