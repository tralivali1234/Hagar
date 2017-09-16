using System.Diagnostics;

namespace Hagar.Analyzers.Helpers
{
    internal class Break
    {
        [Conditional("DEBUG")]
        internal static void IfDebug()
        {
            Debugger.Break();
        }
    }
}
