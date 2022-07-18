using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class InspectorReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        bool isReadOnly;
        if (Application.isPlaying) isReadOnly = (attribute as InspectorReadOnlyAttribute).playing == AccessMode.ReadOnly;
        else                       isReadOnly = (attribute as InspectorReadOnlyAttribute).editing == AccessMode.ReadOnly;

        bool parentEnabled = GUI.enabled;
        if (isReadOnly) GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = parentEnabled;
    }
}