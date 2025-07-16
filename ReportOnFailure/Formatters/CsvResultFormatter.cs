namespace ReportOnFailure.Formatters;

using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CsvResultFormatter : IResultFormatter
{
    public string Format(IReadOnlyCollection<Dictionary<string, object?>> data)
    {
        var csvBuilder = new StringBuilder();
        if (data.Count == 0)
        {
            return string.Empty;
        }

        var headers = data.First().Keys;
        csvBuilder.AppendLine(string.Join(",", headers.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));
        foreach (var row in data)
        {
            csvBuilder.AppendLine(string.Join(",", row.Values.Select(v => $"\"{v?.ToString()?.Replace("\"", "\"\"")}\"")));
        }
        return csvBuilder.ToString();
    }
}