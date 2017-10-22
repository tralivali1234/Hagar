using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyModel;

namespace Hagar.CodeGenerator.MSBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine(DependencyContext.Default);
            while(!Debugger.IsAttached) Thread.Sleep(1000);
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <ProjectFile> <OutputFile>");
                return -2;
            }

            SetupDepsFilesForAppDomain(AppDomain.CurrentDomain);

            var generator = new ProjectCodeGenerator
            {
                ProjectFile = args[0],
                OutputFile = args[1]
            };

            Console.WriteLine(string.Join(" ", Environment.GetCommandLineArgs()));
            Console.WriteLine($"ProjectFile: \"{generator.ProjectFile}\"");
            Console.WriteLine($"OutputFile: \"{generator.OutputFile}\"");

            var result = generator.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (!result) return -1;
            return 0;
        }

        private static void SetupDepsFilesForAppDomain(AppDomain appDomain)
        {
            var thisAssemblyPath = new Uri(typeof(Program).Assembly.CodeBase).LocalPath;
            // Specify the location of dependency context files.
            var codegenDepsFile = Path.Combine(Path.GetDirectoryName(thisAssemblyPath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(thisAssemblyPath)}.deps.json");
            var depsFiles = new List<string>();
            if (File.Exists(codegenDepsFile)) depsFiles.Add(codegenDepsFile);
            if (depsFiles.Count > 0)
            {
                appDomain.SetData("APP_CONTEXT_DEPS_FILES", string.Join(";", depsFiles));
            }
        }
    }
}
