using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildWebGL
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Ball States/Build WebGL")]
    public static void Build()
    {
        AssetDatabase.ImportAsset("Assets/Resources/Sprites", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);

        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        var c = cam.AddComponent<Camera>();
        c.orthographic = true;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color32(186, 214, 255, 255);
        cam.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0, 0, -10);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AlwaysInclude("Sprites/Default");

        PlayerSettings.companyName = "TechEurope2026";
        PlayerSettings.productName = "Ball States";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.template = "PROJECT:Sky";
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Minimal);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "../webgl",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("BUILD RESULT: " + report.summary.result + " size=" + report.summary.totalSize);
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    static void AlwaysInclude(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return;
        var gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
    }
}
