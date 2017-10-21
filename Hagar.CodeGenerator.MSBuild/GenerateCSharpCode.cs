using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Hagar.CodeGenerator.MSBuild
{
    public class GenerateCode : MSBuildTask
    {
        private static readonly int[] SuppressCompilerWarnings =
        {
            162, // CS0162 - Unreachable code detected.
            219, // CS0219 - The variable 'V' is assigned but its value is never used.
            414, // CS0414 - The private field 'F' is assigned but its value is never used.
            618, // CS0616 - Member is obsolete.
            649, // CS0649 - Field 'F' is never assigned to, and will always have its default value.
            693, // CS0693 - Type parameter 'type parameter' has the same name as the type parameter from outer type 'T'
            1591, // CS1591 - Missing XML comment for publicly visible type or member 'Type_or_Member'
            1998 // CS1998 - This async method lacks 'await' operators and will run synchronously
        };

        public string ProjectFile { get; set; }

        public string OutputFile { get; set; }

        public override bool Execute()
        {
            //while (!Debugger.IsAttached) Thread.Sleep(TimeSpan.FromSeconds(1));
            return this.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task<bool> ExecuteAsync(CancellationToken cancellation)
        {
            var compilation = await LoadProject(this.ProjectFile, cancellation);
            var generator = new CodeGenerator(compilation);
            var syntax = generator.GenerateCode(cancellation).NormalizeWhitespace();
            var source = syntax.ToFullString();
            using (var sourceWriter = new StreamWriter(this.OutputFile))
            {
                sourceWriter.WriteLine("#if !EXCLUDE_GENERATED_CODE");
                foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning disable {warningNum}");
                await sourceWriter.WriteLineAsync(source);
                foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning restore {warningNum}");
                sourceWriter.WriteLine("#endif");
            }

            return true;
        }

        private static Task<Compilation> LoadProject(string projectFilePath, CancellationToken cancellationToken)
        {
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(projectFilePath);
            var workspace = analyzer.GetWorkspace();
            var project = workspace.CurrentSolution.Projects.Single();
            return project.GetCompilationAsync(cancellationToken);
        }
    }
}