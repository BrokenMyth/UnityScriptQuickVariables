using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ScriptVariableInjectorGUI : EditorWindow
{
    string v = "";
    private string variableName = "";
    private string variableType = "int"; // 默认类型为 int
    private List<string> variableTypes;
    private bool isManagingCustomTypes = false; // 用于管理自定义类型的标志

    private MonoBehaviour targetScript;
    private const string CustomTypesKey = "CustomVariableTypes"; // 用于保存自定义类型的键
    private const string LastUsedTypeKey = "LastUsedVariableType"; // 用于保存上次使用的类型的键
    private int selectedTypeIndex;
    // 定义需要检查的命名空间和程序集
    string[] assemblyStrings = { "UnityEngine.UI" };
    
    [MenuItem("CONTEXT/MonoBehaviour/Add Variable to Script")]
    static void AddVariableToScript(MenuCommand command)
    {
        // 获取到当前选中的脚本对象
        MonoBehaviour targetScript = (MonoBehaviour)command.context;
        
        // 打开自定义的对话框
        Open(targetScript);
    }
    public static void Open(MonoBehaviour targetScript)
    {
        var window = GetWindow<ScriptVariableInjectorGUI>("Add Variable");
        window.targetScript = targetScript;
        window.LoadVariableTypes(); // 加载类型列表
        window.Show();
    }

    private void LoadVariableTypes()
    {
        // 预定义的类型
        variableTypes = new List<string> { "int", "float", "string", "bool", "GameObject", "Transform" };

        // 加载自定义类型
        string customTypesString = EditorPrefs.GetString(CustomTypesKey, "");
        if (!string.IsNullOrEmpty(customTypesString))
        {
            var customTypes = customTypesString.Split(';');
            variableTypes.AddRange(customTypes); // 添加自定义类型到类型列表
        }
    }

    private void SaveCustomType(string customType)
    {
        // 加载已有的自定义类型
        string customTypesString = EditorPrefs.GetString(CustomTypesKey, "");
        // 将新的类型加入现有的类型字符串
        if (!customTypesString.Contains(customType))
        {
            if (!string.IsNullOrEmpty(customTypesString))
            {
                customTypesString += ";";
            }

            customTypesString += customType;

            // 保存新的自定义类型
            EditorPrefs.SetString(CustomTypesKey, customTypesString);
        }
    }

    private void RemoveCustomType(string customType)
    {
        // 加载已有的自定义类型
        string customTypesString = EditorPrefs.GetString(CustomTypesKey, "");
        var customTypesList = new List<string>(customTypesString.Split(';'));

        // 删除指定的类型
        if (customTypesList.Contains(customType))
        {
            customTypesList.Remove(customType);
            customTypesString = string.Join(";", customTypesList);

            // 保存修改后的类型列表
            EditorPrefs.SetString(CustomTypesKey, customTypesString);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Variable", EditorStyles.boldLabel);

        if (!isManagingCustomTypes)
        {
            // 加载上次使用的类型
            if (variableType == "int" || variableType == "")
            {
                variableType = EditorPrefs.GetString(LastUsedTypeKey, "int");
            }

            // 显示变量类型的下拉框
            selectedTypeIndex = EditorGUILayout.Popup("Variable Type",
                System.Array.IndexOf(variableTypes.ToArray(), variableType), variableTypes.ToArray());
            if (selectedTypeIndex < 0 || selectedTypeIndex >= variableTypes.Count)
            {
                selectedTypeIndex= 0;
            }
            variableType = variableTypes[selectedTypeIndex];
            // GUILayout.Space(10);
            // 输入变量名称
            variableName = EditorGUILayout.TextField("Variable Name", variableName);

            if (GUILayout.Button("Create Variable"))
            {
                if (string.IsNullOrEmpty(variableName))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a valid variable name.", "OK");
                }
                else
                {
                    // 保存上次使用的类型
                    EditorPrefs.SetString(LastUsedTypeKey, variableType);
                    AddVariableToScript();
                    Close(); // 关闭窗口
                }
            }

            // 添加管理自定义类型的按钮
            if (GUILayout.Button("Manage Custom Types"))
            {
                isManagingCustomTypes = true;
            }
        }
        else
        {
            // 显示自定义类型的管理界面
            GUILayout.Label("Manage Custom Types", EditorStyles.boldLabel);

            // 遍历显示所有自定义类型
            string customTypesString = EditorPrefs.GetString(CustomTypesKey, "");
            var customTypesList = new List<string>(customTypesString.Split(';'));
            foreach (var customType in customTypesList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(customType);
                if (GUILayout.Button("Delete"))
                {
                    RemoveCustomType(customType); // 删除自定义类型
                    variableTypes.Remove(customType); // 从类型列表中删除
                    // isManagingCustomTypes = false; // 刷新界面
                }
                GUILayout.EndHorizontal();
            }
            // GUILayout.BeginVertical();
            // 输入变量名称
            v = EditorGUILayout.TextField("Custom", v);
            if (GUILayout.Button("Create"))
            {
                if (string.IsNullOrEmpty(v))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a valid type name.", "OK");
                }
                else
                {
                    SaveCustomType(v); // 保存自定义类型
                    variableTypes.Add(v); // 添加到下拉框列表
                    variableType = v; // 设为当前选择的类型
                    isManagingCustomTypes = false; // 刷新界面
                }
            }
            if (GUILayout.Button("Back"))
            {
                isManagingCustomTypes = false; // 返回变量添加界面
            }
        }
    }

    private void AddVariableToScript()
    {
        // 获取脚本的路径
        MonoScript script = MonoScript.FromMonoBehaviour(targetScript);
        string scriptPath = AssetDatabase.GetAssetPath(script);

        // 读取脚本内容
        string scriptContent = System.IO.File.ReadAllText(scriptPath);
        
        // 获取变量类型的命名空间
        string variableNamespace = GetNamespaceForType(variableType, assemblyStrings);
        // 如果发现了相关命名空间
        if (variableNamespace != null)
        {
            // 检查并添加 using 语句
            string usingStatement = $"using {variableNamespace};\n";
            if (!scriptContent.Contains(usingStatement))
            {
                // 在文件开头添加 using 语句
                int usingInsertIndex = scriptContent.IndexOf("namespace") > 0 ? 
                    scriptContent.IndexOf("namespace") - 1 : 0;
                scriptContent = scriptContent.Insert(usingInsertIndex, usingStatement);
            }
        }
        // 添加变量声明到脚本的末尾
        string variableDeclaration = $"\npublic {variableType} {variableName};";
        // 插入变量声明到类声明之后
        int classDeclarationIndex = scriptContent.IndexOf("{") + 1;
        scriptContent = scriptContent.Insert(classDeclarationIndex, variableDeclaration);

        // 将修改后的内容写回到脚本文件
        System.IO.File.WriteAllText(scriptPath, scriptContent);

        // 刷新 Unity 以加载修改后的脚本
        AssetDatabase.Refresh();
    }
    // 获取类型所在的命名空间
    private string GetNamespaceForType(string typeName, string[] assemblyStrings)
    {
        foreach (string assemblyString in assemblyStrings)
        {
            string[] classNames = GetClassNamesInNamespace(assemblyString);
            if (classNames.Contains(typeName))
            {
                return assemblyString;
            }
        }
        return null; // 如果没有找到类型
    }
    private string[] GetClassNamesInNamespace(string namespaceName)
    {
        var classNames = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace == namespaceName && type.IsClass)
            .Select(type => type.Name)
            .ToArray();

        return classNames;
    }
}