using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArcaneSpire.EditorTools
{
    /// <summary>
    /// Command-line WebGL build for GitHub Pages:
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod ArcaneSpire.EditorTools.WebGLBuild.Build [-buildOutput Builds/WebGL]
    /// GitHub Pages serves files without Content-Encoding headers, so the build uses gzip with the
    /// decompression fallback (the loader decompresses in JavaScript). Exits 0 on success, 1 on failure.
    /// </summary>
    public static class WebGLBuild
    {
        private const string DefaultOutput = "Builds/WebGL";

        /// <summary>Builds every enabled scene in the build settings to the WebGL output folder.</summary>
        [MenuItem("Arcane Spire/Build WebGL (GitHub Pages)")]
        public static void Build()
        {
            string output = ArgValue("-buildOutput") ?? DefaultOutput;
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("WebGLBuild: no enabled scenes in the build settings.");
                Exit(1);
                return;
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"WebGLBuild: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors -> {output}");
            Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
