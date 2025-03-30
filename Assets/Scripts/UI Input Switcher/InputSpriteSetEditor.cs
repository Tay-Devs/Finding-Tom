#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Custom inspector for InputSpriteSet to show more options
[CustomEditor(typeof(InputSpriteSet))]
public class InputSpriteSetEditor : Editor
{
    private SerializedProperty deviceTypeProp;
    private SerializedProperty inputSpritesProp;
    private SerializedProperty ignoreInputPathProp;
    private SerializedProperty suppressWarningsProp;

    private void OnEnable()
    {
        deviceTypeProp = serializedObject.FindProperty("deviceType");
        inputSpritesProp = serializedObject.FindProperty("inputSprites");
        ignoreInputPathProp = serializedObject.FindProperty("ignoreInputPath");
        suppressWarningsProp = serializedObject.FindProperty("suppressWarnings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(deviceTypeProp);
        EditorGUILayout.PropertyField(ignoreInputPathProp);
        EditorGUILayout.PropertyField(suppressWarningsProp);
        
        // Help box to explain path handling
        EditorGUILayout.HelpBox(
            "When 'Ignore Input Path' is enabled, inputs like 'Player/Interact' will match sprites named just 'Interact'.\n\n" +
            "Input names should match either the full path (Player/Interact) or just the action name (Interact).\n\n" +
            "Enable 'Suppress Warnings' to hide console warnings when sprites are not found.", 
            MessageType.Info);

        // Input sprites list
        EditorGUILayout.PropertyField(inputSpritesProp);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif