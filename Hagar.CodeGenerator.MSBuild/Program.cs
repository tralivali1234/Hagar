using System;
using System.Threading;

namespace Hagar.CodeGenerator.MSBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
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

            var result = generator.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (!result) return -1;
            return 0;
        }
    }
}
