namespace ReportOnFailure.Reporters;

using Microsoft.Data.SqlClient;
using System.Data.Common;

/// <summary>
/// A concrete database reporter for SQL Server databases.
/// It uses the Microsoft.Data.SqlClient provider and inherits the common query execution logic from <see cref="BaseDbReporter"/>.
/// </summary>
public class SqlServerReporter : BaseDbReporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerReporter"/> class.
    /// It provides the SQL Server-specific <see cref="DbProviderFactory"/> to the base class.
    /// </summary>
    public SqlServerReporter() : base(SqlClientFactory.Instance)
    {
    }
}