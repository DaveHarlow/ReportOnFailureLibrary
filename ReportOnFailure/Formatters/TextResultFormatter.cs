namespace ReportOnFailure.Formatters;

using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TextResultFormatter : IResultFormatter
{
    public string Format(IReadOnlyCollection<Dictionary<string, object?>> data)
    {
        var textBuilder = new StringBuilder();
        foreach (var row in data)
        {
            textBuilder.AppendLine(string.Join(" | ", row.Select(kvp => $"{kvp.Key}: {kvp.Value ?? "NULL"}")));
            textBuilder.AppendLine(new string('-', 20));
        }
        return textBuilder.ToString();
    }
}