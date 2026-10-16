using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a flag or a state that is tested and then changed in two separate steps (<c>if (!started) { started = true; ... }</c>,
    /// <c>if (state == Idle) { state = Running; }</c>): two threads can both pass the test, and both go on. Nulls are left to DSA007 (lazy initialization).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA050Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA050";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA050AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA050AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA050AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA050.md");

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IFieldSymbol, HashSet<SyntaxNode>> _fieldUsers = new();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
        }

        private static void AnalyzeIf(SyntaxNodeAnalysisContext context)
        {
            var statement = (IfStatementSyntax)context.Node;

            // syntactic prefilter: nearly every if of a code base stops here
            var condition = DSA050Condition.TryParse(statement.Condition);
            if (condition == null || !AssignsTheName(statement, condition.Name))
                return;

            var settings = DSA050Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(statement.SyntaxTree));
            if (AnalyzersUtils.IsFileExcluded(statement.SyntaxTree.FilePath, settings.ExcludedFilePatterns))
                return;

            if (context.SemanticModel.GetSymbolInfo(condition.Access, context.CancellationToken).Symbol is not IFieldSymbol { IsConst: false, IsReadOnly: false } field ||
                !IsStateType(field.Type) ||
                (field.IsVolatile && !settings.ReportVolatileFields) ||
                IsExcluded(field, settings))
                return;

            var claim = FindClaim(statement, condition, field, context);
            if (claim == null || !IsKindEnabled(claim.Value.Kind, settings))
                return;

            var member = statement.Ancestors().FirstOrDefault(static a => a is MemberDeclarationSyntax);
            if (IsExemptedContext(statement, settings) || !IsShared(member, field, context, settings) || Matches(GetMemberName(member), settings.ExcludedMemberNamePatterns))
                return;

            var message = string.Format(
                Resources.Culture,
                Resources.ResourceManager.GetString("DSA050Message" + claim.Value.Kind, Resources.Culture) + (field.IsVolatile ? Resources.DSA050VolatileNote : string.Empty),
                field.Name,
                claim.Value.AssignedText,
                claim.Value.ObservedText);
            var header = TextSpan.FromBounds(statement.IfKeyword.SpanStart, statement.CloseParenToken.Span.End);
            context.ReportDiagnostic(Diagnostic.Create(_rule, Location.Create(statement.SyntaxTree, header), message));
        }

        // ---- the claim: the field is tested, then changed to another value ------------------------------------------

        private static bool AssignsTheName(IfStatementSyntax statement, string name) =>
            ContainsAssignmentTo(statement.Statement, name) || (statement.Else != null && ContainsAssignmentTo(statement.Else, name));

        private static bool ContainsAssignmentTo(SyntaxNode branch, string name)
        {
            foreach (var node in branch.DescendantNodesAndSelf())
            {
                if (node is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && DSA050Condition.GetName(assignment.Left) == name)
                    return true;
            }

            return false;
        }

        private static (string Kind, string AssignedText, string ObservedText)? FindClaim(IfStatementSyntax statement, DSA050Condition condition, IFieldSymbol field, SyntaxNodeAnalysisContext context)
        {
            var model = context.SemanticModel;
            var observedThen = condition.GetObserved(thenBranch: true, field.Type.SpecialType == SpecialType.System_Boolean, model, context.CancellationToken);
            var observedElse = condition.GetObserved(thenBranch: false, field.Type.SpecialType == SpecialType.System_Boolean, model, context.CancellationToken);

            return FindClaimInBranch(statement.Statement, observedThen, condition, field, context) ??
                   (statement.Else == null ? null : FindClaimInBranch(statement.Else.Statement, observedElse, condition, field, context));
        }

        private static (string Kind, string AssignedText, string ObservedText)? FindClaimInBranch(SyntaxNode branch, DSA050Observed observed, DSA050Condition condition, IFieldSymbol field, SyntaxNodeAnalysisContext context)
        {
            if (observed == null)
                return null;

            foreach (var assignment in branch.DescendantNodesAndSelf(static node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).OfType<AssignmentExpressionSyntax>())
            {
                if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) || DSA050Condition.GetName(assignment.Left) != condition.Name ||
                    !SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol, field))
                    continue;

                var assigned = context.SemanticModel.GetConstantValue(assignment.Right, context.CancellationToken);
                if (assigned.HasValue && !Equals(assigned.Value, observed.Value))
                    return (GetKind(field, observed.Value), assignment.Right.ToString(), observed.Text);
            }

            return null;
        }

        private static string GetKind(IFieldSymbol field, object observed)
        {
            if (field.Type.SpecialType != SpecialType.System_Boolean)
                return "Transition";

            return (bool)observed ? "Consume" : "SetOnce";
        }

        private static bool IsStateType(ITypeSymbol type) => type.SpecialType == SpecialType.System_Boolean || type.TypeKind == TypeKind.Enum || type.SpecialType is
            SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Int16 or SpecialType.System_Byte or SpecialType.System_UInt32 or SpecialType.System_UInt16;

        // ---- the options --------------------------------------------------------------------------------------------

        private static bool IsKindEnabled(string kind, DSA050Settings settings) => kind switch
        {
            "SetOnce" => settings.ReportSetOnce,
            "Consume" => settings.ReportConsume,
            _ => settings.ReportStateTransitions,
        };

        private static bool IsExemptedContext(SyntaxNode statement, DSA050Settings settings)
        {
            // the nearest boundary decides: a lambda or a local function can run later, from anywhere
            var boundary = statement.Ancestors().FirstOrDefault(static a => a is LockStatementSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax);
            return (settings.SkipInsideLock && boundary is LockStatementSyntax) ||
                   (settings.SkipConstructors && statement.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax) is ConstructorDeclarationSyntax);
        }

        /// <summary>A member that is not private can be called by any thread; a private one matters when another member uses the field too, or the field is static or volatile.</summary>
        private static bool IsShared(SyntaxNode member, IFieldSymbol field, SyntaxNodeAnalysisContext context, DSA050Settings settings)
        {
            if (!settings.RequireSharedAccess || field.IsStatic || field.IsVolatile)
                return true;

            var symbol = context.SemanticModel.GetDeclaredSymbol(member, context.CancellationToken);
            if (symbol == null || symbol.DeclaredAccessibility != Accessibility.Private)
                return true;

            return UsedByOtherMembers(field, member, context.Compilation, context.CancellationToken);
        }

        private static bool UsedByOtherMembers(IFieldSymbol field, SyntaxNode member, Compilation compilation, CancellationToken cancellationToken)
        {
            var owners = _fieldUsers.GetValue(field.OriginalDefinition, key => FindUsers(key, compilation, cancellationToken));
            foreach (var owner in owners)
            {
                if (owner != member)
                    return true;
            }

            return false;
        }

        /// <summary>The members (constructors and initializers excluded) that use a field.</summary>
        private static HashSet<SyntaxNode> FindUsers(IFieldSymbol field, Compilation compilation, CancellationToken cancellationToken)
        {
            var owners = new HashSet<SyntaxNode>();
            var index = TypeIdentifierIndex.Get(field.ContainingType, compilation);
            foreach (var name in index.Find(field.Name, cancellationToken))
            {
                var owner = name.Ancestors().FirstOrDefault(static a => a is MemberDeclarationSyntax);
                if (owner != null && owner is not (ConstructorDeclarationSyntax or FieldDeclarationSyntax) && !owners.Contains(owner) &&
                    SymbolEqualityComparer.Default.Equals(index.GetModel(name.SyntaxTree).GetSymbolInfo(name, cancellationToken).Symbol, field))
                    owners.Add(owner);
            }

            return owners;
        }

        private static string GetMemberName(SyntaxNode member) => member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            _ => string.Empty,
        };

        private static bool IsExcluded(IFieldSymbol field, DSA050Settings settings)
        {
            var type = field.ContainingType;
            return Matches(field.Name, settings.ExcludedFieldNamePatterns) ||
                   Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
                   (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));
        }

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }

    /// <summary>The value a field is known to have in a branch of an <c>if</c>, and how it is written.</summary>
    internal sealed class DSA050Observed
    {
        internal DSA050Observed(object value, string text)
        {
            Value = value;
            Text = text;
        }

        internal object Value { get; }

        internal string Text { get; }
    }

    /// <summary>The condition of an <c>if</c> that tests a single field: <c>x</c>, <c>!x</c>, <c>x == K</c>, <c>x != K</c> (or <c>K == x</c>, <c>K != x</c>).</summary>
    internal sealed class DSA050Condition
    {
        private readonly ExpressionSyntax _constant;
        private readonly bool _negated;
        private readonly bool _isComparison;

        private DSA050Condition(ExpressionSyntax access, ExpressionSyntax constant, bool negated, bool isComparison)
        {
            Access = access;
            Name = GetName(access);
            _constant = constant;
            _negated = negated;
            _isComparison = isComparison;
        }

        internal ExpressionSyntax Access { get; }

        internal string Name { get; }

        internal static DSA050Condition TryParse(ExpressionSyntax condition)
        {
            condition = Unparenthesize(condition);
            if (GetName(condition) != null)
                return new DSA050Condition(condition, null, negated: false, isComparison: false);

            if (condition is PrefixUnaryExpressionSyntax { OperatorToken.RawKind: (int)SyntaxKind.ExclamationToken } not && GetName(Unparenthesize(not.Operand)) != null)
                return new DSA050Condition(Unparenthesize(not.Operand), null, negated: true, isComparison: false);

            if (condition is not BinaryExpressionSyntax binary || !(binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)))
                return null;

            var negated = binary.IsKind(SyntaxKind.NotEqualsExpression);
            var left = Unparenthesize(binary.Left);
            var right = Unparenthesize(binary.Right);
            if (GetName(left) != null)
                return new DSA050Condition(left, right, negated, isComparison: true);

            return GetName(right) != null ? new DSA050Condition(right, left, negated, isComparison: true) : null;
        }

        /// <summary>The simple name a field is accessed with (<c>x</c>, <c>this.x</c>), or null for any other expression.</summary>
        internal static string GetName(ExpressionSyntax expression) => Unparenthesize(expression) switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name } => name.Identifier.ValueText,
            _ => null,
        };

        private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression;
        }

        /// <summary>What the field is known to hold in the then-branch or in the else-branch, or null when the condition does not say.</summary>
        internal DSA050Observed GetObserved(bool thenBranch, bool isBoolean, SemanticModel model, System.Threading.CancellationToken cancellationToken)
        {
            if (!_isComparison)
            {
                // x: true in the then-branch; !x: false in the then-branch
                var value = thenBranch != _negated;
                return new DSA050Observed(value, value ? "true" : "false");
            }

            var constant = model.GetConstantValue(_constant, cancellationToken);
            if (!constant.HasValue)
                return null;

            // x == K: K in the then-branch; x != K: K in the else-branch; for a bool, the other branch holds the opposite value
            if (thenBranch != _negated)
                return new DSA050Observed(constant.Value, _constant.ToString());

            return isBoolean ? new DSA050Observed(!(bool)constant.Value, ((bool)constant.Value) ? "false" : "true") : null;
        }
    }
}
