using System;
using System.Threading;
using System.Threading.Tasks;
using Hagar.CodeGenerator;

namespace HagarCodeGen
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
        static async Task MainAsync(string[] args)
        {
            var result = await new Generator().GenerateCode(@"C:\dev\Hagar\Samples\MyPocos\MyPocos.csproj", CancellationToken.None);
            Console.WriteLine(result);
        }
    }
}
