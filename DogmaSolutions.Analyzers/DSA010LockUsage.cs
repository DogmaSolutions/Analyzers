using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// How a private lock object is used by the type that declares it: who else takes it (<see cref="OtherCriticalSections"/>), whether it is
    /// signaled with <c>Monitor.Pulse</c> and whether it can be reached by anything else (<see cref="IsEscaped"/>).
    /// </summary>
    internal sealed class DSA010LockUsage
    {
        private const string MonitorType = "System.Threading.Monitor";

        private static readonly ConditionalWeakTable<IFieldSymbol, DSA010LockUsage> _cache = new();

        /// <summary>Critical sections that do more than one plain access, and other uses of the monitor (<c>Wait</c>, <c>Enter</c>...).</summary>
        internal int OtherCriticalSections { get; private set; }

        /// <summary><c>Monitor.Pulse</c> or <c>PulseAll</c> is called on the lock: threads wait for it.</summary>
        internal bool IsSignaled { get; private set; }

        /// <summary>The lock is passed around, returned, compared or reassigned: its identity is not under the control of the type alone.</summary>
        internal bool IsEscaped { get; private set; }

        /// <param name="currentModel">The semantic model of the code being analyzed: it already knows the code of its tree, which is not bound twice.</param>
        internal static DSA010LockUsage Get(IFieldSymbol lockField, SemanticModel currentModel, CancellationToken cancellationToken) =>
            _cache.GetValue(lockField, field => Analyze(field, currentModel, cancellationToken));

        // ---- the occurrences of the private fields of a type ---------------------------------------------------------------

        /// <summary>Where the private fields of a type are named in it, found by walking the type once for all of them (a type may have dozens of locks).</summary>
        private sealed class FieldOccurrences
        {
            internal FieldOccurrences(Dictionary<string, List<IdentifierNameSyntax>> byName, Dictionary<SyntaxTree, SemanticModel> models)
            {
                ByName = byName;
                Models = models;
            }

            internal Dictionary<string, List<IdentifierNameSyntax>> ByName { get; }

            internal Dictionary<SyntaxTree, SemanticModel> Models { get; }
        }

        private static readonly ConditionalWeakTable<INamedTypeSymbol, FieldOccurrences> _occurrences = new();

        private static FieldOccurrences FindOccurrences(INamedTypeSymbol type, SemanticModel currentModel, CancellationToken cancellationToken)
        {
            var names = new HashSet<string>(type.GetMembers().OfType<IFieldSymbol>().Where(field => field.DeclaredAccessibility == Accessibility.Private).Select(field => field.Name));
            var byName = new Dictionary<string, List<IdentifierNameSyntax>>();
            var models = new Dictionary<SyntaxTree, SemanticModel>();

            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                models[reference.SyntaxTree] = reference.SyntaxTree == currentModel.SyntaxTree ? currentModel : currentModel.Compilation.GetSemanticModel(reference.SyntaxTree);
                foreach (var name in reference.GetSyntax(cancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>().Where(identifier => names.Contains(identifier.Identifier.ValueText)))
                {
                    if (!byName.TryGetValue(name.Identifier.ValueText, out var list))
                        byName[name.Identifier.ValueText] = list = new List<IdentifierNameSyntax>();

                    list.Add(name);
                }
            }

            return new FieldOccurrences(byName, models);
        }

        private static DSA010LockUsage Analyze(IFieldSymbol lockField, SemanticModel currentModel, CancellationToken cancellationToken)
        {
            var usage = new DSA010LockUsage();
            var occurrences = _occurrences.GetValue(lockField.ContainingType, type => FindOccurrences(type, currentModel, cancellationToken));

            // the lock is named at least where it is used, but a lock that is not named anywhere is simply a lock nobody uses
            var names = occurrences.ByName.TryGetValue(lockField.Name, out var found) ? found : new List<IdentifierNameSyntax>();
            foreach (var name in names)
            {
                var model = occurrences.Models[name.SyntaxTree];
                if (SymbolEqualityComparer.Default.Equals((model.GetSymbolInfo(name, cancellationToken).Symbol as IFieldSymbol)?.OriginalDefinition, lockField))
                    usage.Classify(name, model, cancellationToken);
            }

            return usage;
        }

        private void Classify(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
        {
            // this.lock is the same thing as lock
            ExpressionSyntax occurrence = name.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access && access.Name == name ? access : name;

            switch (occurrence.Parent)
            {
                case LockStatementSyntax lockStatement:
                    if (!DSA010Pattern.TryMatch(lockStatement, model, cancellationToken, out _, out _))
                        OtherCriticalSections++;
                    break;
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }:
                    ClassifyMonitorCall(invocation, model, cancellationToken);
                    break;
                case AssignmentExpressionSyntax assignment when assignment.Left == occurrence && assignment.Ancestors().OfType<ConstructorDeclarationSyntax>().Any():
                    break;
                default:
                    IsEscaped = true;
                    break;
            }
        }

        private void ClassifyMonitorCall(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method || (method.ContainingType == null || SymbolNames.GetFullName(method.ContainingType) != MonitorType))
                IsEscaped = true;
            else if (method.Name is "Pulse" or "PulseAll")
                IsSignaled = true;
            else
                OtherCriticalSections++;
        }
    }
}
