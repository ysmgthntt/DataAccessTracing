namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database data adapter.
    /// </summary>
    public sealed class DataAdapterInfo : DbObjectInfo
    {
        internal DataAdapterInfo() : base(DbObjectKind.DataAdapter) { }
    }
}
