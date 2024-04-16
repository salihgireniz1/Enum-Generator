using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

[CreateAssetMenu(fileName = "New Enum Generator", menuName = "Scriptable Objects/Enum Generator")]
public class EnumGenerator : ScriptableObject
{
    /// <summary>
    /// The name of the enum to be generated.
    /// </summary>
    [Tooltip("The name of the enum to be generated.")]
    public string enumName;

    /// <summary>
    /// An optional namespace for the enum.
    /// </summary>
    [Tooltip("An optional namespace for the enum.")]
    public string namespaceName;

    /// <summary>
    /// An optional path to specify where the enum file should be generated. If left empty, the default Assets folder will be used.
    /// </summary>
    [Tooltip("The path where the enum file should be generated. Leave empty to default to the Assets folder.")]
    public string path;

    /// <summary>
    /// An array of enum members to be included in the generated enum.
    /// </summary>
    [Tooltip("The members of the enum.")]
    public string[] enumMembers;

    /// <summary>
    /// Updates the enums in the project.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateEnums()
    {
        if (string.IsNullOrEmpty(enumName))
        {
            Debug.LogError("Enum name is not provided.");
            return;
        }

        // Search for any existing files with the same enum name across all directories.
        string searchPattern = $"{enumName}.cs";
        string[] files = Directory.GetFiles(Application.dataPath, searchPattern, SearchOption.AllDirectories);

        // Delete existing enum files if any are found.
        foreach (string filePath in files)
        {
            File.Delete(filePath);
        }

        // Refresh the Asset Database to update the Unity Editor with the file changes.
        AssetDatabase.Refresh();

        // Generate the enum file at the determined file path.
        string filePathToCreate = DetermineFilePath();
        await GenerateEnumFile(filePathToCreate);
    }

    /// <summary>
    /// Determines the file path to create the enum file based on the provided path.
    /// </summary>
    /// <returns>The fully qualified path where the enum file will be created.</returns>
    private string DetermineFilePath()
    {
        string _path = "";
        if (!string.IsNullOrEmpty(path))
        {
            // Normalize the path to ensure it does not start with 'Assets' as Application.dataPath already includes it
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                _path = path.Substring("Assets/".Length);
            }
            return Path.Combine(Application.dataPath, _path, $"{enumName}.cs");
        }
        else
        {
            // Default to the 'Assets' folder if no path is provided
            return Path.Combine(Application.dataPath, $"{enumName}.cs");
        }
    }

    /// <summary>
    /// Generates the enum file at the specified path with the provided members and optional namespace.
    /// </summary>
    /// <param name="filePath">The file path where the enum file will be created.</param>
    /// <returns>A task that represents the asynchronous operation of writing the file.</returns>
    private async Task GenerateEnumFile(string filePath)
    {
        string enumScript = "";

        // Start the namespace block if a namespace is provided.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            enumScript += $"namespace {namespaceName}\n{{\n";
        }

        // Define the enum with its members.
        enumScript += $"public enum {enumName} {{\n";
        for (int i = 0; i < enumMembers.Length; i++)
        {
            enumScript += $"\t{enumMembers[i]}" + (i < enumMembers.Length - 1 ? ",\n" : "\n");
        }
        enumScript += "}\n";

        // Close the namespace block if it was opened.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            enumScript += "}\n";
        }

        // Attempt to write the enum file to disk and log the outcome.
        try
        {
            await File.WriteAllTextAsync(filePath, enumScript);
            Debug.Log($"Enum {enumName} generated successfully at {filePath}");
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to generate enum {enumName}: {e.Message}");
        }
    }
}