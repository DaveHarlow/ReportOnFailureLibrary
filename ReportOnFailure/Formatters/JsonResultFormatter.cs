namespace ReportOnFailure.Formatters;

using System.Collections.Generic;
using System.Text.Json;

public class JsonResultFormatter : IResultFormatter
{
    public string Format(IReadOnlyCollection<Dictionary<string, object?>> data)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }
}