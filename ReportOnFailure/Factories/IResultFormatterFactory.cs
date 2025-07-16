using ReportOnFailure.Enums;
using ReportOnFailure.Formatters;

namespace ReportOnFailure.Factories;

public interface IResultFormatterFactory
{
    IResultFormatter CreateFormatter(ResultsFormat format);
}