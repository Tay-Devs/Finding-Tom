#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Custom inspector for InputPromptUI to improve usability
[CustomEditor(typeof(InputPromptUI))]
public class InputPromptUIEditor : Editor
{
    private SerializedProperty inputActionProp;
    private SerializedProperty promptTypeProp;
    private SerializedProperty promptTextProp;
    private SerializedProperty promptImageProp;
    private SerializedProperty keyboardSpritesProp;
    private SerializedProperty playstationSpritesProp;
    private SerializedProperty xboxSpritesProp;
    private SerializedProperty proControllerSpritesProp;

    private void OnEnable()
    {
        inputActionProp = serializedObject.FindProperty("inputAction");
        promptTypeProp = serializedObject.FindProperty("promptType");
        promptTextProp = serializedObject.FindProperty("promptText");
        promptImageProp = serializedObject.FindProperty("promptImage");
        keyboardSpritesProp = serializedObject.FindProperty("keyboardSprites");
        playstationSpritesProp = serializedObject.FindProperty("playstationSprites");
        xboxSpritesProp = serializedObject.FindProperty("xboxSprites");
        proControllerSpritesProp = serializedObject.FindProperty("proControllerSprites");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(inputActionProp);
        EditorGUILayout.PropertyField(promptTypeProp);

        InputPromptType promptType = (InputPromptType)promptTypeProp.enumValueIndex;

        // Only show text field if we're using Text or Both
        if (promptType == InputPromptType.Text || promptType == InputPromptType.Both)
        {
            EditorGUILayout.PropertyField(promptTextProp);
        }

        // Only show image field if we're using Image or Both
        if (promptType == InputPromptType.Image || promptType == InputPromptType.Both)
        {
            EditorGUILayout.PropertyField(promptImageProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sprite Sets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(keyboardSpritesProp);
            EditorGUILayout.PropertyField(playstationSpritesProp);
            EditorGUILayout.PropertyField(xboxSpritesProp);
            EditorGUILayout.PropertyField(proControllerSpritesProp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

// Custom editor window to help create InputSpriteSets
public class InputSpriteSetCreator : EditorWindow
{
    private InputDeviceType deviceType = InputDeviceType.Keyboard;
    private string newSetName = "New Input Sprite Set";
    private List<KeyValuePair<string, Sprite>> inputSprites = new List<KeyValuePair<string, Sprite>>();
    private Vector2 scrollPosition;
    private string newInputName = "";
    private Sprite newInputSprite = null;

    [MenuItem("Tools/Input System/Input Sprite Set Creator")]
    public static void ShowWindow()
    {
        GetWindow<InputSpriteSetCreator>("Input Sprite Set Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Input Sprite Set", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        newSetName = EditorGUILayout.TextField("Set Name", newSetName);
        deviceType = (InputDeviceType)EditorGUILayout.EnumPopup("Device Type", deviceType);

        EditorGUILayout.Space();
        GUILayout.Label("Input Sprites", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Display existing input sprites
        for (int i = 0; i < inputSprites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            var pair = inputSprites[i];
            EditorGUILayout.LabelField(pair.Key, GUILayout.Width(150));
            EditorGUILayout.ObjectField(pair.Value, typeof(Sprite), false);
            
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                inputSprites.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUILayout.Label("Add New Input Sprite", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        newInputName = EditorGUILayout.TextField("Input Name", newInputName);
        newInputSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", newInputSprite, typeof(Sprite), false);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add Input Sprite") && !string.IsNullOrEmpty(newInputName) && newInputSprite != null)
        {
            inputSprites.Add(new KeyValuePair<string, Sprite>(newInputName, newInputSprite));
            newInputName = "";
            newInputSprite = null;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Sprite Set"))
        {
            CreateSpriteSet();
        }
    }

    private void CreateSpriteSet()
    {
        if (string.IsNullOrEmpty(newSetName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the sprite set.", "OK");
            return;
        }

        if (inputSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please add at least one input sprite.", "OK");
            return;
        }

        // Create the scriptable object
        InputSpriteSet spriteSet = ScriptableObject.CreateInstance<InputSpriteSet>();
        spriteSet.deviceType = deviceType;

        // Add the input sprites
        foreach (var pair in inputSprites)
        {
            InputSprite inputSprite = new InputSprite
            {
                inputName = pair.Key,
                sprite = pair.Value
            };
            spriteSet.inputSprites.Add(inputSprite);
        }

        // Save the scriptable object
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Input Sprite Set",
            newSetName + ".asset",
            "asset",
            "Save the input sprite set as an asset");

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(spriteSet, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = spriteSet;

        // Reset the input sprites list
        inputSprites.Clear();
        newSetName = "New Input Sprite Set";
    }
}
#endif