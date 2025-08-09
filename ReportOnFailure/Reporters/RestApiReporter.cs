namespace ReportOnFailure.Reporters;

using Enums;
using System.Web;

public class RestApiReporter : BaseApiReporter<RestApiReporter>, IRestApiReporter
{

    public ApiHttpMethod Method { get; set; } = ApiHttpMethod.GET;
    public Dictionary<string, object> QueryParameters { get; set; } = new();
    public Dictionary<string, string> FormData { get; set; } = new();


    public RestApiReporter WithMethod(ApiHttpMethod method)
    {
        Method = method;
        return this;
    }

    public RestApiReporter WithQueryParameter(string name, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        QueryParameters[name] = value;
        return this;
    }

    public RestApiReporter WithQueryParameters(Dictionary<string, object> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        foreach (var param in parameters)
        {
            QueryParameters[param.Key] = param.Value;
        }
        return this;
    }

    public RestApiReporter WithFormData(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        FormData[name] = value;
        RequestContentType = ContentType.ApplicationFormUrlEncoded;
        return this;
    }

    public RestApiReporter WithFormData(Dictionary<string, string> formData)
    {
        ArgumentNullException.ThrowIfNull(formData);
        foreach (var data in formData)
        {
            FormData[data.Key] = data.Value;
        }
        RequestContentType = ContentType.ApplicationFormUrlEncoded;
        return this;
    }


    protected override void HandleApiKeyInQuery(string keyName, string keyValue)
    {
        QueryParameters[keyName] = keyValue;
    }


    public string BuildFullUrl()
    {
        var baseUrl = BaseUrl.TrimEnd('/');
        var endpoint = Endpoint.TrimStart('/');
        var fullUrl = $"{baseUrl}/{endpoint}";

        if (QueryParameters.Count == 0)
            return fullUrl;

        var queryString = string.Join("&",
            QueryParameters.Select(kvp =>
                $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value.ToString())}"));

        return $"{fullUrl}?{queryString}";
    }
}