using System.Reflection;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides base information about a database object.
    /// </summary>
    public abstract class DbObjectInfo
    {
        /// <summary>
        /// Gets the unique identifier of the object.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the kind of database object.
        /// </summary>
        public DbObjectKind Kind { get; }

        /// <summary>
        /// Gets the method that created this object.
        /// </summary>
        /// <remarks>
        /// This property may return <see langword="null" /> if the calling method
        /// cannot be determined, including when called from an asynchronous method.
        /// </remarks>
        public MethodBase? CreationMethod { get; }

        internal DbObjectInfo(DbObjectKind kind)
        {
            Id = Guid.NewGuid();
            Kind = kind;
            CreationMethod = DataAccessTrace.GetCallerMethod(5);
        }

        private bool _used;
        internal bool IsUsed => _used;
        internal void Used() => _used = true;
    }
}
