using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Hagar.CodeGenerator;
using Microsoft.CodeAnalysis;

namespace HagarCodeGen
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
        static async Task MainAsync(string[] args)
        {
            var cancellation = CancellationToken.None;
            var compilation = await LoadProject(@"C:\dev\Hagar\Samples\MyPocos\MyPocos.csproj", cancellation);
            var generator = new CodeGenerator(compilation);
            var syntax = generator.GenerateCode(cancellation).NormalizeWhitespace();
            var source = syntax.ToFullString();
            Console.WriteLine(source);
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
