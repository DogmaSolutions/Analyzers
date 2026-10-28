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
        internal DSA010Finding(string messageResourceName, string fieldName, string lockName, string typeSentence, string remedy)
        {
            MessageResourceName = messageResourceName;
            FieldName = fieldName;
            LockName = lockName;
            TypeSentence = typeSentence;
            Remedy = remedy;
        }

        internal string MessageResourceName { get; }

        internal string FieldName { get; }

        internal string LockName { get; }

        /// <summary>What is true of the type of the field, and on which platforms (32-bit and 64-bit processes), about reading and writing it atomically.</summary>
        internal string TypeSentence { get; }

        /// <summary>What can be used instead of the lock, for the types the CLR may read or write torn.</summary>
        internal string Remedy { get; }
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
            if (lockKind == null)
                return null;

            var (typeSentence, remedy) = DSA010Types.Describe(field.Type);
            return new DSA010Finding(DSA010Scenario.GetMessageResourceName(access, lockKind.Value, fieldKind.Value), field.Name, lockName, typeSentence, remedy);
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

        /// <summary>The width in bits of the types that are 32 bits wide or less.</summary>
        private static readonly ImmutableDictionary<SpecialType, int> _bits = ImmutableDictionary<SpecialType, int>.Empty
            .Add(SpecialType.System_Boolean, 8)
            .Add(SpecialType.System_SByte, 8)
            .Add(SpecialType.System_Byte, 8)
            .Add(SpecialType.System_Char, 16)
            .Add(SpecialType.System_Int16, 16)
            .Add(SpecialType.System_UInt16, 16)
            .Add(SpecialType.System_Int32, 32)
            .Add(SpecialType.System_UInt32, 32)
            .Add(SpecialType.System_Single, 32);

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

        /// <summary>
        /// What the message says about the type of the field: whether the CLR reads and writes it as one indivisible operation, and on which
        /// platforms; and, for the types it may read or write torn, what can be used instead of a lock.
        /// </summary>
        internal static (string TypeSentence, string Remedy) Describe(ITypeSymbol type)
        {
            var name = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var underlying = type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } enumType } ? enumType : null;
            var special = (underlying ?? type).SpecialType;

            return DescribeNativeWord(type, name, special) ?? DescribeNarrow(name, underlying, special) ?? DescribeWide(type, name, underlying, special);
        }

        /// <summary>A reference, a pointer, an <c>nint</c>: the size of the native word of the process.</summary>
        private static (string TypeSentence, string Remedy)? DescribeNativeWord(ITypeSymbol type, string name, SpecialType special)
        {
            if (type.IsReferenceType)
                return (Format(Resources.DSA010TypeReference, name), string.Empty);

            return type is IPointerTypeSymbol or IFunctionPointerTypeSymbol || special is SpecialType.System_IntPtr or SpecialType.System_UIntPtr
                ? (Format(Resources.DSA010TypeNativeWord, name), string.Empty)
                : null;
        }

        /// <summary>A type of 32 bits or less, or an enum backed by one.</summary>
        private static (string TypeSentence, string Remedy)? DescribeNarrow(string name, ITypeSymbol underlying, SpecialType special)
        {
            if (!_bits.TryGetValue(special, out var bits))
                return null;

            var sentence = underlying == null
                ? Format(Resources.DSA010TypeSmall, name, bits)
                : Format(Resources.DSA010TypeSmallEnum, name, underlying.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), bits);
            return (sentence, string.Empty);
        }

        /// <summary>A 64-bit value (torn in a 32-bit process), a decimal, a struct or a generic type parameter (torn on every platform).</summary>
        private static (string TypeSentence, string Remedy) DescribeWide(ITypeSymbol type, string name, ITypeSymbol underlying, SpecialType special)
        {
            var other = Format(Resources.DSA010RemedyOther, name);
            if (special is SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double)
            {
                return underlying == null
                    ? (Format(Resources.DSA010TypeWide64, name), GetRemedy(special, name))
                    : (Format(Resources.DSA010TypeWide64Enum, name, underlying.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)), other);
            }

            if (special == SpecialType.System_Decimal)
                return (Format(Resources.DSA010TypeDecimal, name), other);

            return (Format(type.TypeKind == TypeKind.TypeParameter ? Resources.DSA010TypeParameter : Resources.DSA010TypeStruct, name), other);
        }

        private static string GetRemedy(SpecialType special, string name) =>
            special switch
            {
                SpecialType.System_Int64 => Format(Resources.DSA010RemedyLong, name),
                SpecialType.System_UInt64 => Format(Resources.DSA010RemedyULong, name),
                _ => Format(Resources.DSA010RemedyDouble, name),
            };

        private static string Format(string format, params object[] arguments) => string.Format(Resources.Culture, format, arguments);
    }
}
