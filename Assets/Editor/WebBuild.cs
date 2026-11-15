using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ganryujima.Editor
{
    public static class WebBuild
    {
        [MenuItem("Ganryujima/Build browser game")]
        public static void Build()
        {
            ContentSetup.Prepare();
            PolishSetup.Prepare();
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable("GANRYUJIMA_WEB_DIR") ?? "Builds/WebGL");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { ProjectSetup.ScenePath };
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Web build failed: " + report.summary.result);
            FinishPage(output);
            File.Copy("CREDITS.md", Path.Combine(output, "CREDITS.md"), true);
            Debug.Log("GANRYUJIMA WEB BUILD PASS: " + output);
        }
        public static void FinishPage(string output)
        {
            string page = Path.Combine(output, "index.html");
            string html = File.ReadAllText(page);
            html = html.Replace("</head>", "<style>html,body{width:100%;height:100%;overflow:hidden;background:#13201e}#unity-canvas{width:100vw!important;height:100vh!important;display:block;outline:none}#loading{position:fixed;inset:45% 0 auto;color:#eee2c6;font:18px sans-serif;pointer-events:none;z-index:1}</style></head>");
            html = html.Replace("<canvas id=", "<div id=\"loading\" role=\"status\">Loading…</div><canvas id=");
            html = html.Replace("}).then((unityInstance) => {", "}, (progress) => {document.getElementById('loading').textContent = 'Loading ' + Math.round(progress * 100) + '%';}).then((unityInstance) => {document.getElementById('loading').remove();");
            File.WriteAllText(page, html);
        }
    }
}
