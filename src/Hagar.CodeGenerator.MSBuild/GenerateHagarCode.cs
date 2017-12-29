using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Hagar.CodeGenerator.MSBuild
{
    public class GenerateHagarCode : MSBuildTask
    {
        public string ProjectPath { get; set; }
        public string ProjectGuid { get; set; }
        public string OutputType { get; set; }
        public string TargetPath { get; set; }
        public string[] Compile { get; set; }
        public string[] Reference { get; set; }

        public override bool Execute()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    this.Log.LogMessage(MessageImportance.High, $"Asm: {asm.GetName().FullName}");
                }
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


                string projectName = Path.GetFileNameWithoutExtension(ProjectPath);
                ProjectId projectId = !string.IsNullOrEmpty(ProjectGuid) && Guid.TryParse(ProjectGuid, out var projectIdGuid)
                    ? ProjectId.CreateFromSerialized(projectIdGuid)
                    : ProjectId.CreateNewId();


                this.Log.LogMessage(MessageImportance.High, $"ProjectGuid: {ProjectGuid}");
                this.Log.LogMessage(MessageImportance.High, $"ProjectID: {projectId}");

                var languageName = GetLanguageName(ProjectPath);
                var documents = GetDocuments(Compile, projectId).ToList();
                var metadataReferences = GetMetadataReferences(Reference).ToList();

                this.Log.LogMessage(MessageImportance.High, $"Document.Count: {documents.Count}");
                foreach (var doc in documents)
                    this.Log.LogMessage(MessageImportance.High, $"Document: {doc}");
                this.Log.LogMessage(MessageImportance.High, $"Reference.Count: {metadataReferences.Count}");
                foreach (var reference in metadataReferences)
                    this.Log.LogMessage(MessageImportance.High, $"Ref: {reference}");

                var projectInfo = ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    projectName,
                    projectName,
                    languageName,
                    ProjectPath,
                    TargetPath,
                    CreateCompilationOptions(OutputType, languageName),
                    documents: documents,
                    metadataReferences: metadataReferences
                );
                this.Log.LogMessage(MessageImportance.High, $"Project: {projectInfo}");

                var workspace = new AdhocWorkspace();
                workspace.AddProject(projectInfo);
                return true;
            }
            catch (ReflectionTypeLoadException rtle)
            {
                foreach (var ex in rtle.LoaderExceptions)
                    this.Log.LogMessage(MessageImportance.High, $"Exception: {ex}");
                throw;
            }
        }

        private static IEnumerable<DocumentInfo> GetDocuments(string[] sources, ProjectId projectId) =>
            sources
                ?.Where(File.Exists)
                .Select(x => DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(x),
                    loader: TextLoader.From(
                        TextAndVersion.Create(
                            SourceText.From(File.ReadAllText(x)), VersionStamp.Create())),
                    filePath: x))
            ?? Array.Empty<DocumentInfo>();

        private static IEnumerable<MetadataReference> GetMetadataReferences(string[] references) =>
            references
                ?.Where(File.Exists)
                .Select(x => MetadataReference.CreateFromFile(x))
            ?? (IEnumerable<MetadataReference>)Array.Empty<MetadataReference>();


        private static string GetLanguageName(string projectPath)
        {
            switch (Path.GetExtension(projectPath))
            {
                case ".csproj":
                    return LanguageNames.CSharp;
                case ".vbproj":
                    return LanguageNames.VisualBasic;
                default:
                    throw new InvalidOperationException("Could not determine supported language from project path");
            }
        }

        private static CompilationOptions CreateCompilationOptions(string outputType, string languageName)
        {
            OutputKind? kind = null;
            switch (outputType)
            {
                case "Library":
                    kind = OutputKind.DynamicallyLinkedLibrary;
                    break;
                case "Exe":
                    kind = OutputKind.ConsoleApplication;
                    break;
                case "Module":
                    kind = OutputKind.NetModule;
                    break;
                case "Winexe":
                    kind = OutputKind.WindowsApplication;
                    break;
            }

            if (kind.HasValue)
            {
                if (languageName == LanguageNames.CSharp)
                {
                    return new CSharpCompilationOptions(kind.Value);
                }
                if (languageName == LanguageNames.VisualBasic)
                {
                    return new VisualBasicCompilationOptions(kind.Value);
                }
            }

            return null;
        }
    }
}
