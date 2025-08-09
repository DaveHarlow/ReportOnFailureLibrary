namespace ReportOnFailure.Interfaces.Reporters;

using Enums;
using System.Collections.Generic;
using System.Data.Common;

public interface IDbReporter : IReporter
{
    DatabaseType DatabaseType { get; set; }
    string ConnectionString { get; set; }
    int CommandTimeout { get; set; }
    List<DbParameter> Parameters { get; set; }
    string Query { get; set; }

}