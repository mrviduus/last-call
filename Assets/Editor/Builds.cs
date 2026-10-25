using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

// Unity -batchmode -quit -projectPath Game -buildTarget WebGL -executeMethod Builds.WebGL
public static class Builds
{
    const string Scene = "Assets/Main.unity";

    public static void WebGL()
    {
        if (!System.IO.File.Exists(Scene))
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects), Scene);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // plain http server can serve it
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
