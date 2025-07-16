namespace ReportOnFailure.Formatters;

using System.Collections.Generic;

public interface IResultFormatter
{
    string Format(IReadOnlyCollection<Dictionary<string, object?>> data);
}