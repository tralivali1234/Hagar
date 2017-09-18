using System;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagar.Analyzers.Helpers;
using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;
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
    public class GenerateSerializerCodeFix : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == MissingSerializerAnalyzer.Id)
                {
                    var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                    if (string.IsNullOrEmpty(token.ValueText) ||
                        token.IsMissing)
                    {
                        continue;
                    }

                    var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                }
            }
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(MissingSerializerAnalyzer.Id);
    }

    internal class ContainsSerializerAttributeType : QualifiedType
    {
        public ContainsSerializerAttributeType() : base("Hagar.ContainsSerializerAttributeType")
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingSerializerAnalyzer : DiagnosticAnalyzer
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

        private static readonly ContainsSerializerAttributeType ContainsSerializerAttributeType = new ContainsSerializerAttributeType();

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
                    var attributeSymbol = context.SemanticModel.GetSymbolSafe(attribute, context.CancellationToken);
                    if (attributeSymbol == null) continue;
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: Id,
                                title: "Serializer Not Found",
                                messageFormat: "No serializer was found for this type: \"" + attribute.Name + "\" /" + attributeSymbol.ContainingType.MetadataName + "/" +
                                               (attributeSymbol.ContainingType == ContainsSerializerAttributeType),
                                category: "Hagar.Correctness",
                                defaultSeverity: DiagnosticSeverity.Hidden,
                                isEnabledByDefault: true,
                                description: "This type is marked as being serializable, however it does not contain a serializerimplementation."),
                            context.Node.GetLocation()));
                }
            }
        }
    }
}
