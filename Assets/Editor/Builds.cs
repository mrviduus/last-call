using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace Brawler.EditorTools
{
    // Unity -batchmode -quit -projectPath Game -buildTarget WebGL -executeMethod Brawler.EditorTools.Builds.WebGL
    public static class Builds
    {
        const string Scene = "Assets/Main.unity";

        public static void WebGL()
        {
            if (!System.IO.File.Exists(Scene))
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects), Scene);
            // uncompressed so a plain http server can serve it; restored so the build leaves no settings diff
            // hashed file names: the browser can never mix a cached old .wasm with new data
            var compression = PlayerSettings.WebGL.compressionFormat;
            var hashedNames = PlayerSettings.WebGL.nameFilesAsHashes;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            // pass -dev for readable stack traces in the browser console
            var options = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-dev") >= 0
                ? BuildOptions.Development : BuildOptions.None;
            var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/WebGL", BuildTarget.WebGL, options);
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.nameFilesAsHashes = hashedNames;
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
