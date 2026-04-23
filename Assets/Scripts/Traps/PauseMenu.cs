using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _toToggle;

    void Awake()
    {
        _toToggle.SetActive(false);
    }

    void Update(){

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _toToggle.SetActive(!_toToggle.activeSelf);
        }
    }

    public void Button_Leave()
    {
        LobbyUiManager.LeaveLobby();
        SceneManager.LoadScene("MenuScene");
    }
}
