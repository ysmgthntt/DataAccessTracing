using System.Reflection;

namespace DataAccessTracing.Diagnostics
{
    public abstract class DbObjectInfo
    {
        public Guid Id { get; }
        public DbObjectKind Kind { get; }

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
