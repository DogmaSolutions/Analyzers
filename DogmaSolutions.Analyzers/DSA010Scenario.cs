namespace DogmaSolutions.Analyzers
{
    /// <summary>What the critical section does with the field.</summary>
    internal enum DSA010Access
    {
        Read,
        Write,
    }

    /// <summary>How the lock object is used by the rest of the type.</summary>
    internal enum DSA010LockKind
    {
        /// <summary>Only taken around single field accesses: it coordinates nothing.</summary>
        Dedicated,

        /// <summary>Also taken by other critical sections (longer ones, <c>Monitor.Wait</c>...).</summary>
        Shared,

        /// <summary><c>lock (this)</c>.</summary>
        This,
    }

    /// <summary>The nature of the field, as far as the lock is concerned.</summary>
    internal enum DSA010FieldKind
    {
        Plain,

        /// <summary>Wider than the native word of the CPU (<c>long</c>, <c>double</c>, <c>decimal</c>, structs...): a lock may prevent torn reads.</summary>
        Tearable,

        Volatile,

        ReadOnly,
    }

    /// <summary>One message per scenario: each one teaches what is wrong with that specific lock.</summary>
    internal static class DSA010Scenario
    {
        /// <summary>The name of the resource that holds the message of the scenario.</summary>
        internal static string GetMessageResourceName(DSA010Access access, DSA010LockKind lockKind, DSA010FieldKind fieldKind)
        {
            switch (fieldKind)
            {
                case DSA010FieldKind.ReadOnly:
                    return nameof(Resources.DSA010MessageReadonlyField);
                case DSA010FieldKind.Volatile:
                    return access == DSA010Access.Read ? nameof(Resources.DSA010MessageVolatileRead) : nameof(Resources.DSA010MessageVolatileWrite);
                default:
                    return "DSA010Message" + access + lockKind + (fieldKind == DSA010FieldKind.Tearable ? "Tearable" : string.Empty);
            }
        }
    }
}
