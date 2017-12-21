using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Hagar.CodeGenerator.MSBuild
{
    public class ProjectCodeGenerator
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
        
        public async Task<bool> ExecuteAsync(LogLevel logLevel, CancellationToken cancellation)
        {
            using (new AssemblyResolver())
            using (var loggerFactory = new LoggerFactory())
            {
                loggerFactory.AddConsole(logLevel);
                return await Execute(loggerFactory, cancellation);
            }

            async Task<bool> Execute(ILoggerFactory loggerFactory, CancellationToken cancellationToken)
            {
                var compilation = await LoadProject(this.ProjectFile, loggerFactory, cancellationToken);
                
#warning add warning: doesn't reference main assembly
                if (compilation.ReferencedAssemblyNames.All(name => name.Name != "Hagar")) return false;

                var generator = new Hagar.CodeGenerator.CodeGenerator(compilation);
                var syntax = generator.GenerateCode(cancellationToken).NormalizeWhitespace();
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
        }

        private static async Task<Compilation> LoadProject(string projectFilePath, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        {
            var manager = new AnalyzerManager(loggerFactory);
            var analyzer = manager.GetProject(projectFilePath);

            analyzer.GlobalProperties.TryGetValue("DefineConstants", out var defineConstants);
            if (!string.IsNullOrWhiteSpace(defineConstants))
            {
                defineConstants += ";";
            }
            else
            {
                defineConstants = string.Empty;
            }

            analyzer.SetGlobalProperty("DefineConstants", defineConstants + "EXCLUDE_GENERATED_CODE");

            var workspace = analyzer.GetWorkspace();
            var project = workspace.CurrentSolution.Projects.Single();
            var result = await project.GetCompilationAsync(cancellationToken);
            return result;
        }
    }
}