namespace ReportOnFailure.Reporters;

using Microsoft.Data.Sqlite;
using System.Data.Common;

/// <summary>
/// A concrete database reporter for SQLite databases.
/// It uses the Microsoft.Data.Sqlite provider and inherits the common query execution logic from <see cref="BaseDbReporter"/>.
/// </summary>
public class SqlLiteReporter : BaseDbReporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlLiteReporter"/> class.
    /// It provides the SQLite-specific <see cref="DbProviderFactory"/> to the base class.
    /// </summary>
    public SqlLiteReporter() : base(SqliteFactory.Instance)
    {
    }
}