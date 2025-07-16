using ReportOnFailure.Enums;
using ReportOnFailure.Formatters;

namespace ReportOnFailure.Factories;

public class ResultFormatterFactory : IResultFormatterFactory
{
    public IResultFormatter CreateFormatter(ResultsFormat format)
    {
        return format switch
        {
            ResultsFormat.Json => new JsonResultFormatter(),
            ResultsFormat.Csv => new CsvResultFormatter(),
            ResultsFormat.Text => new TextResultFormatter(),
            ResultsFormat.Xml => new NotImplementedResultFormatter(ResultsFormat.Xml.ToString()),
            ResultsFormat.Html => new NotImplementedResultFormatter(ResultsFormat.Html.ToString()),
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                $"Unknown results format: {format}"
            )
        };
    }
}