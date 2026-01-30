using UnityEngine;

public class SelectLevelScreen : MonoBehaviour
{
    
    public void SelectLevel(int levelIndex)
    {
        LevelManager.Singleton.ServerLoadLevel(levelIndex);
    }
}
