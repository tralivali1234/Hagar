using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Hagar.CodeGenerator.MSBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            //while (!Debugger.IsAttached) Thread.Sleep(1000);
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <ArgumentsFile>");
                return -2;
            }

            using (new AssemblyResolver())
            using (var loggerFactory = new LoggerFactory())
            {
                loggerFactory.AddConsole();
                var cmd = new CodeGeneratorCommand(loggerFactory.CreateLogger("Hagar.CodeGenerator"));

                string argsFile = args[0].Trim('"');
                var fileArgs = File.ReadAllLines(argsFile);
                foreach (var arg in fileArgs)
                {
                    var parts = arg.Split(new[] { ':' }, 2);
                    var key = parts[0];
                    var value = parts[1];
                    switch (key)
                    {
                        case nameof(cmd.ProjectGuid):
                            cmd.ProjectGuid = value;
                            break;
                        case nameof(cmd.ProjectPath):
                            cmd.ProjectPath = value;
                            break;
                        case nameof(cmd.OutputType):
                            cmd.OutputType = value;
                            break;
                        case nameof(cmd.TargetPath):
                            cmd.TargetPath = value;
                            break;
                        case nameof(cmd.Compile):
                            cmd.Compile.Add(value);
                            break;
                        case nameof(cmd.Reference):
                            cmd.Reference.Add(value);
                            break;
                        case nameof(cmd.CodeGenOutputFile):
                            cmd.CodeGenOutputFile = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Key '{key}' in argument file is unknown");
                    }
                }

                var ok = cmd.Execute(CancellationToken.None).GetAwaiter().GetResult();
                if (ok) return 0;
            }

            return -1;
        }
    }
}
