using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA032Analyzer : DiagnosticAnalyzer
{
   public const string DiagnosticId = "DSA032";

   internal const int DefaultMaxDuplications = 2;
   internal const int DefaultMinStringLength = 5;
   internal const string MaxDuplicationsOptionKey = "dotnet_diagnostic.DSA032.max_duplications";
   internal const string MinStringLengthOptionKey = "dotnet_diagnostic.DSA032.min_string_length";
   internal const string StringValueProperty = "StringValue";

   private static readonly LocalizableString _title =
      new LocalizableResourceString(nameof(Resources.DSA032AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

   private static readonly LocalizableString _messageFormat =
      new LocalizableResourceString(nameof(Resources.DSA032AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

   private static readonly LocalizableString _description =
      new LocalizableResourceString(nameof(Resources.DSA032AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

   private const string Category = RuleCategories.CodeSmell;

   private static readonly DiagnosticDescriptor _rule = new(
      DiagnosticId,
      _title,
      _messageFormat,
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: _description,
      helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA032.md");

   private static readonly ConcurrentDictionary<AnalyzerConfigOptions, ParsedConfig> _configCache =
      new ConcurrentDictionary<AnalyzerConfigOptions, ParsedConfig>();

   private sealed class ParsedConfig
   {
      public readonly int MaxDuplications;
      public readonly int MinStringLength;

      public ParsedConfig(AnalyzerConfigOptions config)
      {
         if (config.TryGetValue(MaxDuplicationsOptionKey, out var maxDupValue) &&
             int.TryParse(maxDupValue, out var maxDup) &&
             maxDup > 0)
         {
            MaxDuplications = maxDup;
         }
         else
         {
            MaxDuplications = DefaultMaxDuplications;
         }

         if (config.TryGetValue(MinStringLengthOptionKey, out var minLenValue) &&
             int.TryParse(minLenValue, out var minLen) &&
             minLen >= 0)
         {
            MinStringLength = minLen;
         }
         else
         {
            MinStringLength = DefaultMinStringLength;
         }
      }
   }

   private static ParsedConfig GetParsedConfig(SyntaxNodeAnalysisContext context)
   {
      var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
      return _configCache.GetOrAdd(options, o => new ParsedConfig(o));
   }

   public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

   public override void Initialize(AnalysisContext context)
   {
      if (context == null) throw new ArgumentNullException(nameof(context));
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
      context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.ConstructorDeclaration);
   }

   private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
   {
      SyntaxNode body;
      if (context.Node is MethodDeclarationSyntax method)
         body = (SyntaxNode)method.Body ?? method.ExpressionBody;
      else if (context.Node is ConstructorDeclarationSyntax ctor)
         body = (SyntaxNode)ctor.Body ?? ctor.ExpressionBody;
      else
         return;

      if (body == null)
         return;

      var config = GetParsedConfig(context);

      var stringLiterals = body.DescendantNodes()
         .OfType<LiteralExpressionSyntax>()
         .Where(lit => lit.IsKind(SyntaxKind.StringLiteralExpression))
         .ToList();

      var groups = new Dictionary<string, List<LiteralExpressionSyntax>>();
      foreach (var literal in stringLiterals)
      {
         var value = literal.Token.ValueText;
         if (value.Length < config.MinStringLength)
            continue;

         if (!groups.TryGetValue(value, out var list))
         {
            list = new List<LiteralExpressionSyntax>();
            groups[value] = list;
         }

         list.Add(literal);
      }

      foreach (var kvp in groups)
      {
         if (kvp.Value.Count <= config.MaxDuplications)
            continue;

         var properties = ImmutableDictionary.CreateBuilder<string, string>();
         properties.Add(StringValueProperty, kvp.Key);
         var props = properties.ToImmutable();

         foreach (var literal in kvp.Value)
         {
            var diagnostic = Diagnostic.Create(
               descriptor: _rule,
               location: literal.GetLocation(),
               effectiveSeverity: context.GetDiagnosticSeverity(_rule),
               additionalLocations: null,
               properties: props,
               kvp.Key,
               kvp.Value.Count);
            context.ReportDiagnostic(diagnostic);
         }
      }
   }

   internal static string GenerateConstantName(string stringValue)
   {
      var words = new List<string>();
      var currentWord = new StringBuilder();

      foreach (var c in stringValue)
      {
         if (char.IsLetterOrDigit(c))
         {
            currentWord.Append(c);
         }
         else
         {
            if (currentWord.Length > 0)
            {
               words.Add(currentWord.ToString());
               currentWord.Clear();
            }
         }
      }

      if (currentWord.Length > 0)
         words.Add(currentWord.ToString());

      if (words.Count == 0)
         return "StringConstant";

      var result = new StringBuilder();
      foreach (var word in words)
      {
         if (word.Length > 0)
         {
            result.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1)
               result.Append(word.Substring(1));
         }
      }

      var name = result.ToString();

      if (name.Length > 50)
         name = name.Substring(0, 50);

      if (name.Length == 0 || char.IsDigit(name[0]))
         name = "StringConstant";

      if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ||
          SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None)
         name = name + "Value";

      return name;
   }
}
