using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Reporters;

public interface IRestApiReporter : IBaseApiReporter
{
    ApiHttpMethod Method { get; set; }
    Dictionary<string, object> QueryParameters { get; set; }
    Dictionary<string, string> FormData { get; set; }

    string BuildFullUrl();
}