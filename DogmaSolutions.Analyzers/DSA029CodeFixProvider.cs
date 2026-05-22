using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Code fix provider for DSA029: removes the [Required] attribute from non-nullable value type properties,
/// and optionally offers to replace it with [Range(1, T.MaxValue)] for numeric types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA029CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA029CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA029Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var propertyDeclaration = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (propertyDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove [Required] attribute",
                createChangedDocument: ct => RemoveRequiredAttributeAsync(context.Document, propertyDeclaration, ct),
                equivalenceKey: DSA029Analyzer.DiagnosticId + ".Remove"),
            diagnostic);

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;

        var rangeInfo = GetRangeReplacementInfo(propertyDeclaration, semanticModel);
        if (rangeInfo != null)
        {
            var range = rangeInfo.Value;
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Replace with [Range(1, {range.TypeKeywordText}.MaxValue)]",
                    createChangedDocument: ct => ReplaceWithRangeAttributeAsync(context.Document, propertyDeclaration, range.TypeKeyword, ct),
                    equivalenceKey: DSA029Analyzer.DiagnosticId + ".Range"),
                diagnostic);
        }
    }

    internal static (SyntaxKind TypeKeyword, string TypeKeywordText)? GetRangeReplacementInfo(
        PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(propertyDeclaration.Type);
        if (typeInfo.Type == null)
            return null;

        return typeInfo.Type.SpecialType switch
        {
            SpecialType.System_Byte => (SyntaxKind.ByteKeyword, "byte"),
            SpecialType.System_SByte => (SyntaxKind.SByteKeyword, "sbyte"),
            SpecialType.System_Int16 => (SyntaxKind.ShortKeyword, "short"),
            SpecialType.System_UInt16 => (SyntaxKind.UShortKeyword, "ushort"),
            SpecialType.System_Int32 => (SyntaxKind.IntKeyword, "int"),
            SpecialType.System_UInt32 => (SyntaxKind.UIntKeyword, "uint"),
            SpecialType.System_Int64 => (SyntaxKind.LongKeyword, "long"),
            SpecialType.System_UInt64 => (SyntaxKind.ULongKeyword, "ulong"),
            SpecialType.System_Single => (SyntaxKind.FloatKeyword, "float"),
            SpecialType.System_Double => (SyntaxKind.DoubleKeyword, "double"),
            _ => null
        };
    }

    private static async Task<Document> RemoveRequiredAttributeAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var newProperty = RequiredAttributeRemover.RemoveRequiredAttribute(propertyDeclaration, semanticModel);
        if (newProperty == null)
            return document;

        var newRoot = root.ReplaceNode(propertyDeclaration, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ReplaceWithRangeAttributeAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        SyntaxKind typeKeyword,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var newProperty = RequiredAttributeReplacer.ReplaceRequiredWithRange(propertyDeclaration, semanticModel, typeKeyword);
        if (newProperty == null)
            return document;

        var newRoot = root.ReplaceNode(propertyDeclaration, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }
}
