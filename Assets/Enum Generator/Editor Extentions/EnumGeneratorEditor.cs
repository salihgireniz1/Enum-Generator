using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnumGenerator))]
public class EnumGeneratorEditor : Editor
{
    public override async void OnInspectorGUI()
    {
        base.OnInspectorGUI();  // Draws the default inspector

        EnumGenerator generator = (EnumGenerator)target;  // Cast the target to your scriptable object class

        if (GUILayout.Button("Generate/Update Enum Class", GUILayout.Height(25)))
        {
            await generator.UpdateEnums();  // Run the UpdateEnums method asynchronously
        }
    }
}
