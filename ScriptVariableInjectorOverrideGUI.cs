using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(MonoBehaviour), true)]
public class ScriptVariableInjectorOverrideGUI : Editor
{
    private MonoBehaviour targetScript;

    private void OnEnable()
    {
        targetScript = (MonoBehaviour)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // 绘制默认的 Inspector GUI
        
        if (GUILayout.Button("Add Variable to Script"))
        {
            // 打开自定义的对话框
            ScriptVariableInjectorGUI.Open(targetScript);
        }
    }
    
}
