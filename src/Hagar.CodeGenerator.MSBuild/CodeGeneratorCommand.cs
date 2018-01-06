using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

namespace Hagar.CodeGenerator.MSBuild
{
    public class CodeGeneratorCommand
    {
        private const string HagarAssemblyShortName = "Hagar";

        private static readonly int[] SuppressCompilerWarnings =
        {
            162, // CS0162 - Unreachable code detected.
            219, // CS0219 - The variable 'V' is assigned but its value is never used.
            414, // CS0414 - The private field 'F' is assigned but its value is never used.
            618, // CS0616 - Member is obsolete.
            649, // CS0649 - Field 'F' is never assigned to, and will always have its default value.
            693, // CS0693 - Type parameter 'type parameter' has the same name as the type parameter from outer type 'T'
            1591, // CS1591 - Missing XML comment for publicly visible type or member 'Type_or_Member'
            1998 // CS1998 - This async method lacks 'await' operators and will run synchronously
        };

        private readonly ILogger log;

        public CodeGeneratorCommand(ILogger log)
        {
            this.log = log;
        }

        public string ProjectPath { get; set; }
        public string ProjectGuid { get; set; }
        public string OutputType { get; set; }
        public string TargetPath { get; set; }
        public List<string> Compile { get; } = new List<string>();
        public List<string> Reference { get; } = new List<string>();

        public string CodeGenOutputFile { get; set; }

        public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteInternal(cancellationToken);
            }
            catch (ReflectionTypeLoadException rtle)
            {
                foreach (var ex in rtle.LoaderExceptions)
                    this.log.LogInformation($"Exception: {ex}");
                throw;
            }
        }

        private async Task<bool> ExecuteInternal(CancellationToken cancellationToken)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                this.log.LogInformation($"Asm: {asm.GetName().FullName}");
            }
            if (Compile != null)
            {
                this.log.LogInformation($"Compile.Length: {Compile.Count}");
                foreach (var comp in Compile)
                {
                    this.log.LogInformation($"Compile: {comp}");
                }
            }

            if (Reference != null)
            {
                this.log.LogInformation($"Reference.Length: {Reference?.Count}");
                foreach (var comp in Reference)
                {
                    this.log.LogInformation($"Reference: {comp}");
                }
            }


            string projectName = Path.GetFileNameWithoutExtension(ProjectPath);
            ProjectId projectId = !string.IsNullOrEmpty(ProjectGuid) && Guid.TryParse(ProjectGuid, out var projectIdGuid)
                ? ProjectId.CreateFromSerialized(projectIdGuid)
                : ProjectId.CreateNewId();


            this.log.LogInformation($"ProjectGuid: {ProjectGuid}");
            this.log.LogInformation($"ProjectID: {projectId}");

            var languageName = GetLanguageName(ProjectPath);
            var documents = GetDocuments(Compile, projectId).ToList();
            var metadataReferences = GetMetadataReferences(Reference).ToList();

            this.log.LogInformation($"Document.Count: {documents.Count}");
            foreach (var doc in documents)
                this.log.LogInformation($"Document: {doc.FilePath}");
            this.log.LogInformation($"Reference.Count: {metadataReferences.Count}");
            foreach (var reference in metadataReferences)
                this.log.LogInformation($"Ref: {reference.Display}");

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
            this.log.LogInformation($"Project: {projectInfo}");

            var workspace = new AdhocWorkspace();
            workspace.AddProject(projectInfo);

            var project = workspace.CurrentSolution.Projects.Single();
            var compilation = await project.GetCompilationAsync(cancellationToken);

            if (compilation.ReferencedAssemblyNames.All(name => name.Name != HagarAssemblyShortName)) return false;

            var generator = new CodeGenerator(compilation);
            var syntax = generator.GenerateCode(cancellationToken).NormalizeWhitespace();
            var source = syntax.ToFullString();
            using (var sourceWriter = new StreamWriter(this.CodeGenOutputFile))
            {
                sourceWriter.WriteLine("#if !EXCLUDE_GENERATED_CODE");
                foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning disable {warningNum}");
                await sourceWriter.WriteLineAsync(source);
                foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning restore {warningNum}");
                sourceWriter.WriteLine("#endif");
            }

            return true;
        }

        private static IEnumerable<DocumentInfo> GetDocuments(List<string> sources, ProjectId projectId) =>
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

        private static IEnumerable<MetadataReference> GetMetadataReferences(List<string> references) =>
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
