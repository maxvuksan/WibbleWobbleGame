using UnityEngine;

public class Helpers : MonoBehaviour
{


    [SerializeField] private RenderTexture cameraTexture;
    [SerializeField] private RectTransform outputRect;
    [SerializeField] private Canvas canvas;


    [HideInInspector] public float networkPhysicsTickRate = 1f / 60f;
    public LayerMask layerWorldUi;
    public int foregroundRenderingLayer;
    public int uiRenderingLayer;


    public static Helpers Singleton;

    void Awake()
    {
        Singleton = this;
    }


    public Vector2 PixelScreenToWorld(Vector2 input)
    {
        return Camera.main.ScreenToWorldPoint(input);


        // Dynamically get screen resolution
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Scale input to render texture resolution
        float scaleX = cameraTexture.width / screenWidth;
        float scaleY = cameraTexture.height / screenHeight;

        scaleX *= screenWidth / (outputRect.rect.width * canvas.transform.localScale.x);
        scaleY *= screenHeight / (outputRect.rect.height * canvas.transform.localScale.y);

        Vector2 scaledInput = new Vector2(input.x * scaleX, input.y * scaleY);

        // Convert to world position
        Vector3 screenPos = new Vector3(scaledInput.x, scaledInput.y, Mathf.Abs(Camera.main.transform.position.z));
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        return new Vector2(worldPos.x, worldPos.y);
    }


    public static float EaseInOutQuint(float t)
    {
        return t < 0.5f
            ? 16f * t * t * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
    }

    /// <summary>
    /// Calls .SetActive() on every element of an array
    /// </summary>
    public static void SetActiveGameObjectArray(GameObject[] array, bool state)
    {
        for(int i = 0; i < array.Length; i++)
        {
            array[i].SetActive(state);
        }
    }

}
