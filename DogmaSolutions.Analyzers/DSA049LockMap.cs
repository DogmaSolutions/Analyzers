using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>One access to a field of the type: where it is, what it does, and the locks that are held there.</summary>
    internal sealed class DSA049Access
    {
        internal DSA049Access(IdentifierNameSyntax name, IFieldSymbol field, bool isWrite, string memberName, ISet<string> heldLocks)
        {
            Name = name;
            Field = field;
            IsWrite = isWrite;
            MemberName = memberName;
            HeldLocks = heldLocks;
        }

        internal IdentifierNameSyntax Name { get; }

        internal IFieldSymbol Field { get; }

        internal bool IsWrite { get; }

        /// <summary>The member the access is in.</summary>
        internal string MemberName { get; }

        /// <summary>The names of the locks held where the access is: the <c>lock</c> statements around it, and the ones the callers of its private method hold.</summary>
        internal ISet<string> HeldLocks { get; }
    }

    /// <summary>
    /// Reads a type once and tells, for every access to a field of the type, which locks are held. The code of a type is the only code that can touch
    /// a private field, so the picture is complete (partial parts included).
    /// </summary>
    internal sealed class DSA049LockMap
    {
        private const int MaxPasses = 5;

        private readonly INamedTypeSymbol _type;
        private readonly Compilation _compilation;
        private readonly CancellationToken _cancellationToken;
        private readonly DSA049Settings _settings;
        private static readonly ISet<string> _noLocks = new HashSet<string>(); // shared, and never modified

        private readonly Dictionary<MemberDeclarationSyntax, IMethodSymbol> _methodOf = new();
        private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();
        private SyntaxNode[] _roots = Array.Empty<SyntaxNode>();
        private TypeIdentifierIndex _index;

        /// <summary>The locks all the callers of a private method hold.</summary>
        private readonly Dictionary<IMethodSymbol, ISet<string>> _inherited = new(SymbolEqualityComparer.Default);

        /// <summary>The names of the fields that are handed to Interlocked / Volatile (or to any method by reference): their protocol is not a lock.</summary>
        private readonly HashSet<string> _lockFreeFields = new();

        internal DSA049LockMap(INamedTypeSymbol type, Compilation compilation, DSA049Settings settings, CancellationToken cancellationToken)
        {
            _type = type;
            _compilation = compilation;
            _settings = settings;
            _cancellationToken = cancellationToken;
        }

        internal bool HasLocks { get; private set; }

        internal List<DSA049Access> Accesses { get; } = new();

        internal bool IsLockFree(IFieldSymbol field) => _lockFreeFields.Contains(field.Name);

        internal void Build()
        {
            _index = TypeIdentifierIndex.Get(_type, _compilation);
            HasLocks = _index.HasLocks(_cancellationToken);
            if (!HasLocks)
                return;

            _roots = _index.GetRoots(_cancellationToken);
            if (_settings.InferLocksOfPrivateHelpers)
                InferHelperLocks();

            // only the names of the fields of the type are looked at: every other identifier of the type is of no interest, and binding it would be wasted
            foreach (var field in _type.GetMembers().OfType<IFieldSymbol>().Where(static f => !f.IsConst && !f.IsReadOnly && !f.IsImplicitlyDeclared))
            {
                foreach (var name in _index.Find(field.Name, _cancellationToken))
                    CollectAccess(name);
            }
        }

        /// <summary>An identifier of the type itself: a nested type is a type of its own.</summary>
        private bool IsOwn(SyntaxNode node) => Array.IndexOf(_roots, TypeIdentifierIndex.GetOwner(node)) >= 0;

        // ---- the locks ----------------------------------------------------------------------------------------------

        /// <summary>The name of the lock object of a <c>lock</c> statement: a member of the type, <c>this</c>, or the text of any other expression.</summary>
        private static string GetLockName(LockStatementSyntax statement)
        {
            var expression = statement.Expression;
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var name } => name.Identifier.ValueText,
                _ => expression.ToString(),
            };
        }

        private SemanticModel GetModel(SyntaxTree tree)
        {
            if (!_models.TryGetValue(tree, out var model))
                _models[tree] = model = _index.GetModel(tree);

            return model;
        }

        /// <summary>
        /// Walks up from a node once: the <c>lock</c> statements around it in its member, the member itself and the type declaration it belongs to
        /// (which is not the type being analyzed when the node is in a nested type).
        /// </summary>
        private static ISet<string> Locate(SyntaxNode node, out MemberDeclarationSyntax member, out BaseTypeDeclarationSyntax owner)
        {
            HashSet<string> held = null;
            member = null;
            owner = null;
            SyntaxNode child = node;
            foreach (var ancestor in node.Ancestors())
            {
                if (member == null)
                {
                    if (ancestor is LockStatementSyntax lockStatement && lockStatement.Statement == child)
                        (held ??= new HashSet<string>()).Add(GetLockName(lockStatement));
                    else if (ancestor is MemberDeclarationSyntax declaration)
                        member = declaration;
                }

                if (ancestor is BaseTypeDeclarationSyntax typeDeclaration)
                {
                    owner = typeDeclaration;
                    break;
                }

                child = ancestor;
            }

            return held ?? _noLocks;
        }

        /// <summary>The locks held at a node: the <c>lock</c> statements around it in its member, and the ones its member inherits from its callers.</summary>
        private ISet<string> GetHeldLocks(SyntaxNode node, SemanticModel model)
        {
            var held = Locate(node, out var member, out _);
            return AddInherited(held, member, model);
        }

        private ISet<string> AddInherited(ISet<string> held, MemberDeclarationSyntax member, SemanticModel model)
        {
            if (_inherited.Count == 0 || member == null || GetMethod(member, model) is not { } method || !_inherited.TryGetValue(method, out var inherited))
                return held;

            var merged = new HashSet<string>(held);
            merged.UnionWith(inherited);
            return merged;
        }

        private IMethodSymbol GetMethod(MemberDeclarationSyntax member, SemanticModel model)
        {
            if (!_methodOf.TryGetValue(member, out var method))
                _methodOf[member] = method = model.GetDeclaredSymbol(member, _cancellationToken) as IMethodSymbol;

            return method;
        }

        /// <summary>
        /// A private method that is called, and only called, from places where a lock is held holds that lock too. The callers are looked at again until
        /// nothing changes (a helper of a helper), a few times at most.
        /// </summary>
        private void InferHelperLocks()
        {
            var calls = new Dictionary<IMethodSymbol, List<(SyntaxNode Call, SemanticModel Model)>>(SymbolEqualityComparer.Default);
            var escaped = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (var method in _type.GetMembers().OfType<IMethodSymbol>().Where(static m => m.DeclaredAccessibility == Accessibility.Private && m.MethodKind == MethodKind.Ordinary))
            {
                foreach (var name in _index.Find(method.Name, _cancellationToken))
                {
                    var model = GetModel(name.SyntaxTree);
                    if (!IsOwn(name) || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name, _cancellationToken).Symbol?.OriginalDefinition, method.OriginalDefinition))
                        continue;

                    var call = name.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member && member.Name == name ? member : (SyntaxNode)name;
                    if (call.Parent is InvocationExpressionSyntax invocation && invocation.Expression == call)
                    {
                        if (!calls.TryGetValue(method.OriginalDefinition, out var list))
                            calls[method.OriginalDefinition] = list = new List<(SyntaxNode, SemanticModel)>();

                        list.Add((invocation, model));
                    }
                    else
                    {
                        escaped.Add(method.OriginalDefinition);
                    }
                }
            }

            for (var pass = 0; pass < MaxPasses; pass++)
            {
                var changed = false;
                foreach (var entry in calls.Where(pair => !escaped.Contains(pair.Key)))
                {
                    ISet<string> common = null;
                    foreach (var (call, model) in entry.Value)
                    {
                        var held = GetHeldLocks(call, model);
                        if (common == null)
                            common = new HashSet<string>(held);
                        else
                            common.IntersectWith(held);
                    }

                    if (!_inherited.TryGetValue(entry.Key, out var previous) || !previous.SetEquals(common))
                    {
                        _inherited[entry.Key] = common;
                        changed = true;
                    }
                }

                if (!changed)
                    break;
            }
        }

        // ---- the accesses -------------------------------------------------------------------------------------------

        private void CollectAccess(IdentifierNameSyntax name)
        {
            // a nested type is a type of its own
            var held = Locate(name, out var member, out var owner);
            if (Array.IndexOf(_roots, owner) < 0)
                return;

            var model = GetModel(name.SyntaxTree);
            if (model.GetSymbolInfo(name, _cancellationToken).Symbol is not IFieldSymbol { IsConst: false, IsReadOnly: false } field ||
                !SymbolEqualityComparer.Default.Equals(field.ContainingType.OriginalDefinition, _type.OriginalDefinition) ||
                field.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name == "ThreadStaticAttribute"))
                return;

            var access = name.Parent is MemberAccessExpressionSyntax parent && parent.Name == name ? parent : (ExpressionSyntax)name;
            if (IsHandedByReference(access))
                _lockFreeFields.Add(field.Name);

            // nobody else can see the instance while it is being built (or torn down): its accesses are not part of the protocol
            if (member is null or ConstructorDeclarationSyntax or DestructorDeclarationSyntax or FieldDeclarationSyntax ||
                (member is MethodDeclarationSyntax { Identifier.ValueText: "Dispose" }))
                return;

            Accesses.Add(new DSA049Access(name, field, IsWrite(access), GetMemberName(member), AddInherited(held, member, model)));
        }

        private static string GetMemberName(SyntaxNode member) => member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            _ => member.Kind().ToString(),
        };

        private static bool IsHandedByReference(ExpressionSyntax access) =>
            access.Parent is ArgumentSyntax argument && (argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

        private static bool IsWrite(ExpressionSyntax access)
        {
            var current = (SyntaxNode)access;
            while (current.Parent is ParenthesizedExpressionSyntax or ArgumentSyntax { Parent: TupleExpressionSyntax } or TupleExpressionSyntax)
                current = current.Parent;

            return current.Parent switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left == current,
                PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
                PostfixUnaryExpressionSyntax => true,
                _ => false,
            };
        }
    }
}
