using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Hagar.CodeGenerator.MSBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            //while (!Debugger.IsAttached) Thread.Sleep(1000);
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <ProjectFile> <OutputFile>");
                return -2;
            }

            // HACK: https://github.com/daveaglick/Buildalyzer/issues/29

            var dotnetSdkVersion = "2.0.0";
            var extensionsPath = $@"C:\Program Files\dotnet\sdk\{dotnetSdkVersion}";
            var msbuildSdkPath = $@"{extensionsPath}\SDKs";
            Environment.SetEnvironmentVariable("VisualStudioVersion", "15.0");
            Environment.SetEnvironmentVariable("MSBuildExtensionsPath", extensionsPath);
            Environment.SetEnvironmentVariable("MSBuildSDKsPath", msbuildSdkPath);
            Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\");

            // END

            var generator = new ProjectCodeGenerator
            {
                ProjectFile = args[0],
                OutputFile = args[1]
            };

            var result = generator.ExecuteAsync(LogLevel.Information, CancellationToken.None).GetAwaiter().GetResult();
            if (!result) return -1;
            return 0;
        }
    }
}
