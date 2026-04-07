using TMPro;
using UnityEngine;

public class LobbyMembersPlayerEntry : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _playerHeadRenderer;

    public void SetName(string text)
    {
        gameObject.GetComponentInChildren<TextMeshPro>().text = text;        
    }
    public void SetPlayerColour(Color playerColour)
    {
        _playerHeadRenderer.color = playerColour;
    }
}
