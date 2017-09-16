using System;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagar.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
namespace Hagar.Analyzers
{
    /*public class GenerateSerializerCodeFix : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == GenerateSerializerDiagnosticAnalyzer.Id)
                {
                    var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                    if (string.IsNullOrEmpty(token.ValueText) ||
                        token.IsMissing)
                    {
                        continue;
                    }

                    var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                    node.
                }
            }
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GenerateSerializerDiagnosticAnalyzer.Id);
    }*/

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GenerateSerializerDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "HAGAR001";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Serializer implementation missing.",
            messageFormat: "No serializer was found for this type.",
            category: "Hagar.Correctness",
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "This type is marked as being serializable, however no serializer implementation."
#warning add a help link.
            /*, helpLinkUri: */);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(this.HandleDeclaration, SyntaxKind.StructDeclaration);
        }

        private void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis()) return;
            if (!(context.Node is TypeDeclarationSyntax declarationSyntax)) return;


            foreach (var attributeList in declarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: Id,
                                title: $"Attribute {attribute.Name}",
                                messageFormat: "No serializer was found for this type.",
                                category: "Hagar.Correctness",
                                defaultSeverity: DiagnosticSeverity.Hidden,
                                isEnabledByDefault: true,
                                description: "This type is marked as being serializable, however no serializer implementation."),
                            context.Node.GetLocation()));
                }
            }
        }
    }
}
