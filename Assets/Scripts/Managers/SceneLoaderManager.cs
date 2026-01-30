using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadMainScene());
    }

    IEnumerator LoadMainScene()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        SceneManager.LoadScene("MenuScene");
    }
}
