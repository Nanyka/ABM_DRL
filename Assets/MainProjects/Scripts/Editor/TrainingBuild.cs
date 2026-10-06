using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Sugarscape.EditorTools
{
    public static class TrainingBuild
    {
        private const string k_TrainingScene = "Assets/MainProjects/Scenes/SugarscrapeTraining.unity";

        // Headless macOS server build of the training scene, for mlagents-learn --env.
        // Usage: Unity -batchmode -quit -projectPath . -executeMethod Sugarscape.EditorTools.TrainingBuild.BuildServer -buildOutput <path>
        public static void BuildServer()
        {
            var output = "Builds/sugarscape_train_server/ABM_DRL";
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOutput") output = args[i + 1];

            var options = new BuildPlayerOptions
            {
                scenes = new[] { k_TrainingScene },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Training build failed: {report.summary.result}");
        }
    }
}
