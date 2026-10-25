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
        // uncompressed so a plain http server can serve it; restored so the build leaves no settings diff
        var compression = PlayerSettings.WebGL.compressionFormat;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
        PlayerSettings.WebGL.compressionFormat = compression;
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
