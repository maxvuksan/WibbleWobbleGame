#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Allows the IntHundredth type to be drawn and modified in the UnityEditor. Exposes the value as a float
/// This conversion operation is done strictly in the editor serializing the value as a scaled integer which will behave the same on different hardware configurations
/// </summary>
[CustomPropertyDrawer(typeof(IntHundredth))]
public class IntHundredthDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty valueHundredths = property.FindPropertyRelative("ValueHundredths");

        // Show as a float in the inspector, store as int hundredths
        float currentFloat = valueHundredths.intValue / 100f;
        
        EditorGUI.BeginChangeCheck();  // Track if user actually changed something
        float newFloat = EditorGUI.FloatField(position, label, currentFloat);
        
        if (EditorGUI.EndChangeCheck())  // Only write if value changed
        {
            valueHundredths.intValue = Mathf.RoundToInt(newFloat * 100f);
        }

        EditorGUI.EndProperty();
    }
}
#endif