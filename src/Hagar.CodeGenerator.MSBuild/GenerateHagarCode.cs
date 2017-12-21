using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Hagar.CodeGenerator.MSBuild
{
    public class GenerateHagarCode : MSBuildTask
    {
        public string[] Compile { get; set; }
        public string[] Reference { get; set; }

        public override bool Execute()
        {
            if (Compile != null)
            {
                this.Log.LogMessage(MessageImportance.High, $"Compile.Length: {Compile?.Length}");
                foreach (var comp in Compile)
                {
                    this.Log.LogMessage(MessageImportance.High, $"Compile: {comp}");
                }
            }

            if (Reference != null)
            {
                this.Log.LogMessage(MessageImportance.High, $"Reference.Length: {Reference?.Length}");
                foreach (var comp in Reference)
                {
                    this.Log.LogMessage(MessageImportance.High, $"Reference: {comp}");
                }
            }

            return true;
        }
    }
}
