using System;
using FixMath.NET;
using UnityEngine;
using Volatile;

public class Helpers : MonoBehaviour
{

    [SerializeField] private RenderTexture cameraTexture;
    [SerializeField] private RectTransform outputRect;
    [SerializeField] private Canvas canvas;


    [HideInInspector] public float networkPhysicsTickRate = 1f / 60f;

    public int layerWorldUi;
    public LayerMask layerMaskWorldUi;
    public int foregroundRenderingLayer;
    public int uiRenderingLayer;
    public Material RopeMaterial;


    public static Helpers Singleton;

    void Awake()
    {
        Singleton = this;
    }


    /// <summary>
    /// A utility function for creating and enforcing Singleton behaviour on a class
    /// </summary>
    /// <typeparam name="T">The class type to create a singleton from</typeparam>
    /// <param name="Singleton">The Singleton static variable</param>
    /// <param name="callingClass">The instance of the Singleton we wish to promote</param>
    public static void CreateSingleton<T>(ref T Singleton, T callingClass) where T : MonoBehaviour
    {
        if (Singleton != null)
        {
            Debug.LogWarning("Could not create Singleton (" + Singleton.name + ") because another instance already exists");
            Destroy(Singleton.gameObject);
            return;
        }

        Singleton = callingClass;
        DontDestroyOnLoad(Singleton.gameObject);
    }

    public Vector2 PixelScreenToWorld(Vector2 input)
    {
        // Get mouse position in canvas space
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            outputRect,
            input,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out canvasPos
        );
        
        // Normalize to [0,1] range
        float normalizedX = (canvasPos.x / outputRect.rect.width) + 0.5f;
        float normalizedY = (canvasPos.y / outputRect.rect.height) + 0.5f;
        
        // Scale to render texture resolution
        float rtX = normalizedX * cameraTexture.width;
        float rtY = normalizedY * cameraTexture.height;
        
        // Convert to world position using the camera that renders to the texture
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(
            Mathf.Clamp(rtX, -9999, 9999), 
            Mathf.Clamp(rtY, -9999, 9999), 
            Mathf.Abs(Camera.main.transform.position.z)
        ));

        
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

    /// <summary>
    /// Invokes an action, catching an exceptions raised by subscribed events
    /// </summary>
    public static void SafeInvoke(Action action, string functionLabel)
    {
        if (action == null){
            return;
        }

        foreach (var subscriber in action.GetInvocationList())
        {
            // Skip if target is a disabled MonoBehaviour
            if (subscriber.Target is MonoBehaviour behaviour && !behaviour.enabled)
            {
                continue;
            }

            try
            {
                ((Action)subscriber).Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Exception during {functionLabel} from " +
                    $"{subscriber.Target?.GetType().Name}: {ex}");
            }
        }
    }

    /// <summary>
    /// Given a transform component converts a local point to world space deterministcally using Fix64 maths
    /// </summary>
    /// <returns>World space point</returns>
    public static VoltVector2 TransformPointFix64(CustomTransform customTransform, VoltVector2 localPoint)
    {
        VoltVector2 rotatedPoint = RotatePosition(localPoint, customTransform.GetRotationRadiansFix64());
        return customTransform.GetPositionFix64() + rotatedPoint;
    }

    public static VoltVector2 RotatePosition(VoltVector2 localPoint, Fix64 angleRad)
    {
        Fix64 cos = Fix64.Cos(angleRad);
        Fix64 sin = Fix64.Sin(angleRad);

        return new VoltVector2(
                cos * localPoint.x - sin * localPoint.y,
                sin * localPoint.x + cos * localPoint.y
        );
    }

    public static VoltVector2 TransformLocalPositionByParentTransform(CustomTransform parentTransform, VoltVector2 position)
    {
        VoltVector2 transformedPosition = RotatePosition(position, parentTransform.GetRotationRadiansFix64());
        transformedPosition += parentTransform.GetPositionFix64();

        return transformedPosition;
    }

    public static VoltVector2 TransformWorldPositionToLocalPosition(VoltVector2 worldPosition, VoltVector2 objectPosition, Fix64 objectRotationRadians)
    {
        VoltVector2 relativePosition = worldPosition - objectPosition;
        VoltVector2 localPosition = RotatePosition(relativePosition, -objectRotationRadians);
        
        return localPosition;
    }

}
