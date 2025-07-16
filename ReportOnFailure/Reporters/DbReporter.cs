namespace ReportOnFailure.Reporters;

using System;
using System.Data.Common; 
using Interfaces;
using Enums;


public partial class DbReporter : BaseReporter<DbReporter>, IDbReporter
{

    public DatabaseType DatabaseType { get; set; }
    public string ConnectionString { get; set; }
    public string Query { get; set; }
    public int CommandTimeout { get; set; } = 30;

    public List<DbParameter> Parameters { get; set; } = new List<DbParameter>();

    public DbReporter WithDatabaseType(DatabaseType dbType)
    {
        DatabaseType = dbType;
        return this;
    }

    public DbReporter WithConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public DbReporter WithQuery(string query)
    {
        Query = query;
        return this;
    }

    public DbReporter WithCommandTimeout(int timeout)
    {
        CommandTimeout = timeout;
        return this;
    }

    public DbReporter AddParameter(DbParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException(nameof(parameter));
        Parameters.Add(parameter);
        return this;
    }

    public DbReporter AddParameters(IEnumerable<DbParameter> parameters)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        Parameters.AddRange(parameters);
        return this;
    }
}
