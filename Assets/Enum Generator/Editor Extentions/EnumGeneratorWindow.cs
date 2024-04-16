using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// A window for creating and managing Enum Generators.
/// </summary>
public class EnumGeneratorWindow : EditorWindow
{
    /// <summary>
    /// The name of the enum to be generated.
    /// </summary>
    private string enumName = "NewEnum";

    /// <summary>
    /// The namespace for the enum.
    /// </summary>
    private string namespaceName = "";

    /// <summary>
    /// The path for the enum file, relative to Assets/.
    /// </summary>
    private string path = "";

    /// <summary>
    /// A list of enum members to be included in the generated enum.
    /// </summary>
    private List<string> enumMembers = new List<string>();

    /// <summary>
    /// Creates a new instance of EnumGeneratorWindow.
    /// </summary>
    [MenuItem("Tools/Enum Generator")]
    public static void ShowWindow()
    {
        GetWindow<EnumGeneratorWindow>("Enum Generator");
    }

    /// <summary>
    /// Handles the GUI for the window, allowing the user to input enum properties and members.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Create a New Enum Class", EditorStyles.boldLabel);

        // Inputs for enum properties
        enumName = EditorGUILayout.TextField("Enum Name", enumName);
        namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
        path = EditorGUILayout.TextField("Path (relative to Assets/)", path);

        // Handling enum members input dynamically
        GUILayout.Label("Enum Members:");
        if (enumMembers.Count == 0 || !string.IsNullOrEmpty(enumMembers[enumMembers.Count - 1]))
        {
            enumMembers.Add("");
        }

        int removeIndex = -1;
        for (int i = 0; i < enumMembers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            enumMembers[i] = EditorGUILayout.TextField(enumMembers[i]);
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1)
        {
            enumMembers.RemoveAt(removeIndex);
        }

        // Button to create the Enum
        if (GUILayout.Button("Generate Enum Class", GUILayout.Height(25)))
        {
            CreateEnumGenerator();
        }
    }

    /// <summary>
    /// Generates the Enum based on the input properties and members.
    /// </summary>
    private void CreateEnumGenerator()
    {
        if (string.IsNullOrEmpty(enumName))
        {
            EditorUtility.DisplayDialog("Error", "Enum name cannot be empty.", "OK");
            return;
        }

        // Remove empty members before generation
        enumMembers.RemoveAll(string.IsNullOrEmpty);

        // Search for all EnumGenerator assets
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(EnumGenerator)}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnumGenerator existingGenerator = AssetDatabase.LoadAssetAtPath<EnumGenerator>(path);

            if (existingGenerator != null && existingGenerator.enumName == enumName)
            {
                // Automatically highlight the existing generator in the Unity Project window
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existingGenerator;

                if (EditorUtility.DisplayDialog("Existing Generator Found", $"A generator responsible for creating '{enumName}' already exists. Do you want to override it?", "Yes", "No"))
                {
                    UpdateExistingGenerator(existingGenerator);
                }
                return; // Stop checking further, since we have found a matching generator
            }
        }

        // No existing generator found; create a new one
        CreateNewGenerator();
    }

    /// <summary>
    /// Normalizes the path to ensure it does not start with 'Assets/'.
    /// </summary>
    /// <param name="inputPath">The input path to be normalized.</param>
    /// <returns>The normalized path.</returns>
    private string NormalizePath(string inputPath)
    {
        // Normalize the path to ensure it does not start with 'Assets/' since Application.dataPath already includes it
        if (inputPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return inputPath.Substring("Assets/".Length);
        }
        return inputPath;
    }

    /// <summary>
    /// Creates a new EnumGenerator asset.
    /// </summary>
    private void CreateNewGenerator()
    {
        string normalizedPath = NormalizePath(path);

        EnumGenerator generator = CreateInstance<EnumGenerator>();
        generator.enumName = enumName;
        generator.namespaceName = namespaceName;
        generator.path = path;
        generator.enumMembers = enumMembers.ToArray();

        string assetPath = Path.Combine("Assets", normalizedPath, $"{enumName} Generator.asset");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        if (!Directory.Exists(Path.Combine(Application.dataPath, normalizedPath)))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, normalizedPath));
        }

        AssetDatabase.CreateAsset(generator, assetPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = generator;

        // Optionally generate the enum immediately
        generator.UpdateEnums().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("Failed to generate enum: " + t.Exception.Flatten().InnerException.Message);
            }
        });
    }

    /// <summary>
    /// Updates an existing EnumGenerator asset.
    /// </summary>
    /// <param name="existingGenerator">The existing EnumGenerator asset to be updated.</param>
    private void UpdateExistingGenerator(EnumGenerator existingGenerator)
    {
        existingGenerator.namespaceName = namespaceName;
        existingGenerator.path = path;
        existingGenerator.enumMembers = enumMembers.ToArray();

        EditorUtility.SetDirty(existingGenerator);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = existingGenerator;

        // Optionally regenerate the enum file
        existingGenerator.UpdateEnums().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("Failed to regenerate enum: " + t.Exception.Flatten().InnerException.Message);
            }
        });
    }
}