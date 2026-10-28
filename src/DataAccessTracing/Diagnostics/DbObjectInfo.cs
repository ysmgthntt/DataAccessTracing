using System.Diagnostics;
using System.Reflection;

namespace DataAccessTracing.Diagnostics
{
    public abstract class DbObjectInfo
    {
        public Guid ID { get; }
        public DbObjectKind Kind { get; private set; }

        private readonly MethodBase? _creationMethod;

        public DbObjectInfo(DbObjectKind kind)
        {
            ID = Guid.NewGuid();
            Kind = kind;

            StackTrace trace = new StackTrace();
            for (int index = 4; index < trace.FrameCount; index++)
            {
                var method = trace.GetFrame(index).GetMethod();
                var typeName = method.DeclaringType.FullName;
                if (!typeName.StartsWith("DataAccessTracing.", StringComparison.Ordinal) &&
                    !typeName.StartsWith("System.", StringComparison.Ordinal) &&
                    !typeName.StartsWith("Microsoft.", StringComparison.Ordinal))
                {
                    _creationMethod = method;
                    break;
                }
            }

            Kind = kind;
        }

        private bool _used;
        public bool IsUsed => _used;
        public void Used() => _used = true;
    }
}
