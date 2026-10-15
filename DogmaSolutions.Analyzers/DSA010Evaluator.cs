using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What DSA010 found: the scenario (which selects the message) and the names the message talks about.</summary>
    internal sealed class DSA010Finding
    {
        internal DSA010Finding(string messageResourceName, string fieldName, string lockName)
        {
            MessageResourceName = messageResourceName;
            FieldName = fieldName;
            LockName = lockName;
        }

        internal string MessageResourceName { get; }

        internal string FieldName { get; }

        internal string LockName { get; }
    }

    /// <summary>
    /// Decides whether a <c>lock</c> statement is a critical section around a single plain access of a field, in which case it cannot
    /// protect any logic of the class, and in which of the scenarios of DSA010 it is.
    /// </summary>
    internal static class DSA010Evaluator
    {
        internal static DSA010Finding Evaluate(LockStatementSyntax lockStatement, SemanticModel model, DSA010Settings settings, CancellationToken cancellationToken)
        {
            // the cheap, syntactic questions first; the lock usage scan of the type is the last and most expensive one
            var member = DSA010Scope.FindMember(lockStatement, settings, out _);
            if (member == null || IsExcludedByMemberOrFile(member, lockStatement.SyntaxTree, settings))
                return null;

            if (!DSA010Pattern.TryMatch(lockStatement, model, cancellationToken, out var access, out var field))
                return null;

            if ((access == DSA010Access.Read ? !settings.ReportReads : !settings.ReportWrites) || IsExcludedByField(field, settings))
                return null;

            var fieldKind = GetFieldKind(field, access, settings);
            if (fieldKind == null)
                return null;

            var lockKind = GetLockKind(lockStatement, field, model, settings, cancellationToken, out var lockName);
            return lockKind == null ? null : new DSA010Finding(DSA010Scenario.GetMessageResourceName(access, lockKind.Value, fieldKind.Value), field.Name, lockName);
        }

        // ---- exclusions ---------------------------------------------------------------------------------------------

        private static bool IsExcludedByMemberOrFile(SyntaxNode member, SyntaxTree tree, DSA010Settings settings)
        {
            var memberName = DSA010Scope.GetMemberName(member);
            return AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns) ||
                   (memberName != null && Matches(memberName, settings.ExcludedMemberNamePatterns));
        }

        private static bool IsExcludedByField(IFieldSymbol field, DSA010Settings settings)
        {
            var type = field.ContainingType;
            return Matches(field.Name, settings.ExcludedFieldNamePatterns) ||
                   Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
                   IsNamespaceExcluded(type, settings) ||
                   (settings.ExcludedFieldTypes.Length > 0 && settings.ExcludedFieldTypes.Contains(SymbolNames.GetFullName(field.Type))) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));
        }

        /// <summary>The name of the namespace is built only if there is a pattern to match it with.</summary>
        private static bool IsNamespaceExcluded(INamedTypeSymbol type, DSA010Settings settings)
        {
            return settings.ExcludedNamespacePatterns.Length > 0 &&
                   Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns);
        }

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));

        // ---- the field ----------------------------------------------------------------------------------------------

        /// <summary>The nature of the field, or null when the options say the scenario is not to be reported.</summary>
        private static DSA010FieldKind? GetFieldKind(IFieldSymbol field, DSA010Access access, DSA010Settings settings)
        {
            if (field.IsReadOnly)
                return access == DSA010Access.Read && settings.ReportReadonlyFields ? DSA010FieldKind.ReadOnly : null;

            if (field.IsVolatile)
                return settings.ReportVolatileFields ? DSA010FieldKind.Volatile : null;

            if (!DSA010Types.IsTearable(field.Type))
                return DSA010FieldKind.Plain;

            return settings.ReportTearableTypes ? DSA010FieldKind.Tearable : null;
        }

        // ---- the lock -----------------------------------------------------------------------------------------------

        /// <summary>How the lock object is used by the type, or null when it must not be reported (it may be relied upon, or the options say so).</summary>
        private static DSA010LockKind? GetLockKind(LockStatementSyntax lockStatement, IFieldSymbol field, SemanticModel model, DSA010Settings settings, CancellationToken cancellationToken, out string lockName)
        {
            lockName = "this";
            if (lockStatement.Expression is ThisExpressionSyntax)
                return settings.ReportLockOnThis ? DSA010LockKind.This : null;

            var lockField = DSA010Pattern.GetField(lockStatement.Expression, model, cancellationToken);
            if (lockField == null || lockField.DeclaredAccessibility != Accessibility.Private || Matches(lockField.Name, settings.ExcludedLockNamePatterns) ||
                !SymbolEqualityComparer.Default.Equals(lockField.ContainingType.OriginalDefinition, field.ContainingType.OriginalDefinition))
                return null;

            lockName = lockField.Name;
            var usage = DSA010LockUsage.Get(lockField.OriginalDefinition, model, cancellationToken);
            if (usage.IsEscaped || (usage.IsSignaled && settings.SkipSignaledLocks))
                return null;

            if (usage.OtherCriticalSections == 0)
                return DSA010LockKind.Dedicated;

            return settings.ReportSharedLocks ? DSA010LockKind.Shared : null;
        }
    }

    internal static class DSA010Types
    {
        /// <summary>The types the CLR reads and writes atomically on any platform: not wider than 32 bits.</summary>
        private static readonly ImmutableHashSet<SpecialType> _atomicTypes = ImmutableHashSet.Create(
            SpecialType.System_Boolean,
            SpecialType.System_Char,
            SpecialType.System_SByte,
            SpecialType.System_Byte,
            SpecialType.System_Int16,
            SpecialType.System_UInt16,
            SpecialType.System_Int32,
            SpecialType.System_UInt32,
            SpecialType.System_Single,
            SpecialType.System_IntPtr,
            SpecialType.System_UIntPtr);

        /// <summary>
        /// A type that is wider than the native word of the CPU, or made of several fields: reading or writing it is not guaranteed to be
        /// atomic, so a lock may be what keeps a reader from seeing half of an old and half of a new value.
        /// </summary>
        internal static bool IsTearable(ITypeSymbol type)
        {
            if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol || type.IsReferenceType)
                return false;

            var effective = type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlying } ? underlying : type;
            return !_atomicTypes.Contains(effective.SpecialType);
        }
    }
}
