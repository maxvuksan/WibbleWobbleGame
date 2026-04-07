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
            rtX, 
            rtY, 
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
        // Convert transform values to Fix64
        Fix64 posX = customTransform.GetPositionFix64().x;
        Fix64 posY = customTransform.GetPositionFix64().y;
        
        // Rotation
        Fix64 angleRad = customTransform.GetRotationRadiansFix64();
        Fix64 cos = Fix64.Cos(angleRad);
        Fix64 sin = Fix64.Sin(angleRad);
        
        // In our use case do not care about scale...
        
        // Rotate
        Fix64 rotatedX = localPoint.x * cos - localPoint.y * sin;
        Fix64 rotatedY = localPoint.x * sin + localPoint.y * cos;
        
        // Translate
        return new VoltVector2(posX + rotatedX, posY + rotatedY);
    }

    public static VoltVector2 TransformLocalPositionByParentTransform(CustomTransform parentTransform, VoltVector2 position)
    {
        Fix64 cos = Fix64.Cos(parentTransform.GetRotationRadiansFix64());
        Fix64 sin = Fix64.Sin(parentTransform.GetRotationRadiansFix64());

        VoltVector2 transformedPosition = new VoltVector2(
                cos * position.x - sin * position.y,
                sin * position.x + cos * position.y
        );
        
        transformedPosition += parentTransform.GetPositionFix64();

        return transformedPosition;
    }

}
