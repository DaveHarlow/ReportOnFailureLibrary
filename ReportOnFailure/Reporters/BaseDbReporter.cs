namespace ReportOnFailure.Reporters;

using System;
using System.Data.Common; // For DbProviderFactory
using System.Linq;
using System.Text;
using ReportOnFailure.Interfaces;

/// <summary>
/// An abstract base class for database reporters using ADO.NET.
/// It provides the core logic to connect to a database, run a query, and format the results.
/// Concrete implementations must provide a DbProviderFactory for a specific database provider.
/// </summary>
public abstract class BaseDbReporter : IDbReporter
{
    protected readonly DbProviderFactory _factory;

    protected string? ConnectionString { get; private set; }
    protected string? TableName { get; private set; }
    protected string? SchemaName { get; private set; }
    protected string? DatabaseName { get; private set; }
    protected int CommandTimeout { get; private set; } = 30; // Default command timeout in seconds
    protected string? WhereClause { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDbReporter"/> class.
    /// </summary>
    /// <param name="factory">The database provider factory for the specific DBMS.</param>
    protected BaseDbReporter(DbProviderFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    #region IDbReporter Implementation

    public void SetConnectionString(string connectionString) => ConnectionString = connectionString;

    public void SetTableName(string tableName) => TableName = tableName;

    public void SetSchemaName(string schemaName) => SchemaName = schemaName;

    public void SetDatabaseName(string databaseName) => DatabaseName = databaseName;

    public void SetCommandTimeout(int timeout) => CommandTimeout = timeout;

    public void SetWhereClause(string whereClause) => WhereClause = whereClause;

    /// <summary>
    /// Generates the report data by executing the query and returns it as a string.
    /// This method is part of the IReporter interface.
    /// </summary>
    /// <returns>A string containing the report data.</returns>
    public string OutPutData()
    {
        EnsureValidState();

        try
        {
            using var connection = CreateConnection();
            connection.Open();
            return ExecuteQueryAndFormatResults(connection);
        }
        catch (DbException ex)
        {
            throw new ApplicationException("An error occurred while interacting with the database. See inner exception for details.", ex);
        }
    }

    #endregion

    /// <summary>
    /// Creates and configures a database connection.
    /// </summary>
    /// <returns>An open database connection.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a connection cannot be created.</exception>
    protected virtual DbConnection CreateConnection()
    {
        var connection = _factory.CreateConnection() ?? throw new InvalidOperationException("The DbProviderFactory could not create a DbConnection object.");
        connection.ConnectionString = ConnectionString;
        return connection;
    }

    /// <summary>
    /// Executes the query and formats the results.
    /// </summary>
    /// <param name="connection">The open database connection to use.</param>
    /// <returns>A string representation of the query results, typically in a tab-separated format.</returns>
    protected virtual string ExecuteQueryAndFormatResults(DbConnection connection)
    {
        using var command = connection.CreateCommand() ?? throw new InvalidOperationException("The DbConnection could not create a DbCommand object.");
        command.CommandText = BuildQuery();
        command.CommandTimeout = CommandTimeout;

        return ReadData(command);
    }

    private string ReadData(DbCommand command)
    {
        var builder = new StringBuilder();
        using var reader = command.ExecuteReader();

        if (!reader.HasRows) return builder.ToString();

        // Add header row
        var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        builder.AppendLine(string.Join("\t", columnNames));

        builder.Append(GetDataRows(reader));
        return builder.ToString();
    }

    private string GetDataRows(DbDataReader reader)
    {
        var resultBuilder = new StringBuilder();
            // Add data rows
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                resultBuilder.AppendLine(string.Join("\t", values.Select(v => v == DBNull.Value ? "NULL" : v?.ToString())));
            }
        return resultBuilder.ToString();
    }

    private void EnsureValidState()
    {
         if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("Connection string must be set before reporting.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException("Table name must be set before reporting.");
    }
        /// <summary>
    /// Builds the SQL query string.
    /// This method can be overridden by derived classes to handle DBMS-specific syntax for table and schema names.
    /// </summary>
    /// <returns>The complete SQL query string.</returns>
    /// <remarks>
    /// WARNING: This default implementation is vulnerable to SQL injection through the WhereClause property.
    /// For production code, you should enhance this to use parameterized queries.
    /// </remarks>
    protected virtual string BuildQuery()
    {
        // ANSI SQL standard uses double quotes for identifiers.
        // Specific providers like SQL Server use brackets `[]` which can be handled in an override.
        var qualifiedTableName = string.IsNullOrWhiteSpace(SchemaName)
            ? $"\"{TableName}\""
            : $"\"{SchemaName}\".\"{TableName}\"";

        var query = $"SELECT * FROM {qualifiedTableName}";

        if (!string.IsNullOrWhiteSpace(WhereClause))
        {
            query += $" WHERE {WhereClause}";
        }

        return query;
    }
}