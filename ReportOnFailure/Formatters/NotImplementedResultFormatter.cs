namespace ReportOnFailure.Formatters;

using System.Collections.Generic;

public class NotImplementedResultFormatter : IResultFormatter
{
    private readonly string _formatName;

    public NotImplementedResultFormatter(string formatName)
    {
        _formatName = formatName;
    }

    public string Format(IReadOnlyCollection<Dictionary<string, object?>> data)
    {
        return $"Result formatting for '{_formatName}' is not yet implemented.";
    }
}