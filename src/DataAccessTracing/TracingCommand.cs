using System.Data.Common;

namespace DataAccessTracing
{
    internal class TracingCommand
    {
        internal DbCommand InnerCommand;

        public TracingCommand(DbCommand innerCommand, TracingConnection connection)
        {
            InnerCommand = innerCommand;
        }
    }
}
