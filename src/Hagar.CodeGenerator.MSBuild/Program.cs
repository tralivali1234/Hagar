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
