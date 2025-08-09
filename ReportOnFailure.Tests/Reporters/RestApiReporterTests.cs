using ReportOnFailure.Enums;
using ReportOnFailure.Reporters;
using System.Text;

namespace ReportOnFailure.Tests.Reporters;

public class RestApiReporterTests
{
    #region HTTP Method Tests

    [Fact]
    public void WithMethod_SetsMethodCorrectly()
    {

        var reporter = new RestApiReporter();


        var result = reporter.WithMethod(ApiHttpMethod.POST);


        Assert.Equal(ApiHttpMethod.POST, reporter.Method);
        Assert.Same(reporter, result);
    }

    [Fact]
    public void Method_DefaultsToGET()
    {

        var reporter = new RestApiReporter();


        Assert.Equal(ApiHttpMethod.GET, reporter.Method);
    }

    [Theory]
    [InlineData(ApiHttpMethod.GET)]
    [InlineData(ApiHttpMethod.POST)]
    [InlineData(ApiHttpMethod.PUT)]
    [InlineData(ApiHttpMethod.PATCH)]
    [InlineData(ApiHttpMethod.DELETE)]
    [InlineData(ApiHttpMethod.HEAD)]
    [InlineData(ApiHttpMethod.OPTIONS)]
    public void WithMethod_WorksForAllHttpMethods(ApiHttpMethod method)
    {

        var reporter = new RestApiReporter();


        reporter.WithMethod(method);


        Assert.Equal(method, reporter.Method);
    }

    #endregion

    #region Query Parameter Tests

    [Fact]
    public void WithQueryParameter_AddsParameterCorrectly()
    {

        var reporter = new RestApiReporter();


        var result = reporter.WithQueryParameter("key1", "value1");


        Assert.Equal("value1", reporter.QueryParameters["key1"]);
        Assert.Same(reporter, result);
    }

    [Fact]
    public void WithQueryParameter_AcceptsDifferentValueTypes()
    {

        var reporter = new RestApiReporter();


        reporter
            .WithQueryParameter("stringParam", "stringValue")
            .WithQueryParameter("intParam", 42)
            .WithQueryParameter("boolParam", true)
            .WithQueryParameter("doubleParam", 3.14);


        Assert.Equal("stringValue", reporter.QueryParameters["stringParam"]);
        Assert.Equal(42, reporter.QueryParameters["intParam"]);
        Assert.Equal(true, reporter.QueryParameters["boolParam"]);
        Assert.Equal(3.14, reporter.QueryParameters["doubleParam"]);
    }

    [Fact]
    public void WithQueryParameter_OverwritesExistingParameter()
    {

        var reporter = new RestApiReporter();
        reporter.WithQueryParameter("key1", "oldValue");


        reporter.WithQueryParameter("key1", "newValue");


        Assert.Equal("newValue", reporter.QueryParameters["key1"]);
        Assert.Single(reporter.QueryParameters);
    }

    [Fact]
    public void WithQueryParameter_ThrowsArgumentNullException_WhenNameIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithQueryParameter(null!, "value"));
    }

    [Fact]
    public void WithQueryParameter_ThrowsArgumentException_WhenNameIsEmpty()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentException>(() => reporter.WithQueryParameter("", "value"));
    }

    [Fact]
    public void WithQueryParameter_ThrowsArgumentException_WhenNameIsWhitespace()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentException>(() => reporter.WithQueryParameter("   ", "value"));
    }

    [Fact]
    public void WithQueryParameter_ThrowsArgumentNullException_WhenValueIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithQueryParameter("key", null!));
    }

    [Fact]
    public void WithQueryParameters_AddsDictionaryCorrectly()
    {

        var reporter = new RestApiReporter();
        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "value1",
            ["param2"] = 42,
            ["param3"] = true
        };


        var result = reporter.WithQueryParameters(parameters);


        Assert.Equal("value1", reporter.QueryParameters["param1"]);
        Assert.Equal(42, reporter.QueryParameters["param2"]);
        Assert.Equal(true, reporter.QueryParameters["param3"]);
        Assert.Same(reporter, result);
    }

    [Fact]
    public void WithQueryParameters_MergesWithExistingParameters()
    {

        var reporter = new RestApiReporter();
        reporter.WithQueryParameter("existing", "existingValue");

        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "value1",
            ["param2"] = 42
        };


        reporter.WithQueryParameters(parameters);


        Assert.Equal("existingValue", reporter.QueryParameters["existing"]);
        Assert.Equal("value1", reporter.QueryParameters["param1"]);
        Assert.Equal(42, reporter.QueryParameters["param2"]);
        Assert.Equal(3, reporter.QueryParameters.Count);
    }

    [Fact]
    public void WithQueryParameters_OverwritesExistingParametersWithSameKey()
    {

        var reporter = new RestApiReporter();
        reporter.WithQueryParameter("param1", "oldValue");

        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "newValue",
            ["param2"] = 42
        };


        reporter.WithQueryParameters(parameters);


        Assert.Equal("newValue", reporter.QueryParameters["param1"]);
        Assert.Equal(42, reporter.QueryParameters["param2"]);
        Assert.Equal(2, reporter.QueryParameters.Count);
    }

    [Fact]
    public void WithQueryParameters_ThrowsArgumentNullException_WhenParametersIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithQueryParameters(null!));
    }

    [Fact]
    public void WithQueryParameters_HandlesEmptyDictionary()
    {

        var reporter = new RestApiReporter();
        reporter.WithQueryParameter("existing", "value");
        var emptyParameters = new Dictionary<string, object>();


        reporter.WithQueryParameters(emptyParameters);


        Assert.Single(reporter.QueryParameters);
        Assert.Equal("value", reporter.QueryParameters["existing"]);
    }

    #endregion

    #region Form Data Tests

    [Fact]
    public void WithFormData_AddsFormDataCorrectly()
    {

        var reporter = new RestApiReporter();


        var result = reporter.WithFormData("field1", "value1");


        Assert.Equal("value1", reporter.FormData["field1"]);
        Assert.Equal(ContentType.ApplicationFormUrlEncoded, reporter.RequestContentType);
        Assert.Same(reporter, result);
    }

    [Fact]
    public void WithFormData_OverwritesExistingFormData()
    {

        var reporter = new RestApiReporter();
        reporter.WithFormData("field1", "oldValue");


        reporter.WithFormData("field1", "newValue");


        Assert.Equal("newValue", reporter.FormData["field1"]);
        Assert.Single(reporter.FormData);
    }

    [Fact]
    public void WithFormData_ThrowsArgumentNullException_WhenNameIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithFormData(null!, "value"));
    }

    [Fact]
    public void WithFormData_ThrowsArgumentException_WhenNameIsEmpty()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentException>(() => reporter.WithFormData("", "value"));
    }

    [Fact]
    public void WithFormData_ThrowsArgumentException_WhenNameIsWhitespace()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentException>(() => reporter.WithFormData("   ", "value"));
    }

    [Fact]
    public void WithFormData_ThrowsArgumentNullException_WhenValueIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithFormData("field", null!));
    }

    [Fact]
    public void WithFormData_Dictionary_AddsDictionaryCorrectly()
    {

        var reporter = new RestApiReporter();
        var formData = new Dictionary<string, string>
        {
            ["field1"] = "value1",
            ["field2"] = "value2",
            ["field3"] = "value3"
        };


        var result = reporter.WithFormData(formData);


        Assert.Equal("value1", reporter.FormData["field1"]);
        Assert.Equal("value2", reporter.FormData["field2"]);
        Assert.Equal("value3", reporter.FormData["field3"]);
        Assert.Equal(ContentType.ApplicationFormUrlEncoded, reporter.RequestContentType);
        Assert.Same(reporter, result);
    }

    [Fact]
    public void WithFormData_Dictionary_MergesWithExistingFormData()
    {

        var reporter = new RestApiReporter();
        reporter.WithFormData("existing", "existingValue");

        var formData = new Dictionary<string, string>
        {
            ["field1"] = "value1",
            ["field2"] = "value2"
        };


        reporter.WithFormData(formData);


        Assert.Equal("existingValue", reporter.FormData["existing"]);
        Assert.Equal("value1", reporter.FormData["field1"]);
        Assert.Equal("value2", reporter.FormData["field2"]);
        Assert.Equal(3, reporter.FormData.Count);
    }

    [Fact]
    public void WithFormData_Dictionary_OverwritesExistingFormDataWithSameKey()
    {

        var reporter = new RestApiReporter();
        reporter.WithFormData("field1", "oldValue");

        var formData = new Dictionary<string, string>
        {
            ["field1"] = "newValue",
            ["field2"] = "value2"
        };


        reporter.WithFormData(formData);


        Assert.Equal("newValue", reporter.FormData["field1"]);
        Assert.Equal("value2", reporter.FormData["field2"]);
        Assert.Equal(2, reporter.FormData.Count);
    }

    [Fact]
    public void WithFormData_Dictionary_ThrowsArgumentNullException_WhenFormDataIsNull()
    {

        var reporter = new RestApiReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithFormData((Dictionary<string, string>)null!));
    }

    [Fact]
    public void WithFormData_Dictionary_HandlesEmptyDictionary()
    {

        var reporter = new RestApiReporter();
        reporter.WithFormData("existing", "value");
        var emptyFormData = new Dictionary<string, string>();


        reporter.WithFormData(emptyFormData);


        Assert.Single(reporter.FormData);
        Assert.Equal("value", reporter.FormData["existing"]);
        Assert.Equal(ContentType.ApplicationFormUrlEncoded, reporter.RequestContentType);
    }

    #endregion

    #region API Key Handling Tests

    [Fact]
    public void HandleApiKeyInQuery_AddsApiKeyToQueryParameters()
    {

        var reporter = new RestApiReporter();


        reporter.WithApiKey("api_key", "test-key-123", inHeader: false);


        Assert.Equal("test-key-123", reporter.QueryParameters["api_key"]);
    }

    [Fact]
    public void HandleApiKeyInQuery_OverwritesExistingQueryParameter()
    {

        var reporter = new RestApiReporter();
        reporter.WithQueryParameter("api_key", "old-key");


        reporter.WithApiKey("api_key", "new-key", inHeader: false);


        Assert.Equal("new-key", reporter.QueryParameters["api_key"]);
    }

    #endregion

    #region URL Building Tests

    [Fact]
    public void BuildFullUrl_WithoutQueryParameters_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users");


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/users", url);
    }

    [Fact]
    public void BuildFullUrl_WithTrailingSlashInBaseUrl_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com/")
            .WithEndpoint("/users");


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/users", url);
    }

    [Fact]
    public void BuildFullUrl_WithLeadingSlashInEndpoint_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("users");


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/users", url);
    }

    [Fact]
    public void BuildFullUrl_WithBothTrailingAndLeadingSlashes_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com/")
            .WithEndpoint("/users");


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/users", url);
    }

    [Fact]
    public void BuildFullUrl_WithSingleQueryParameter_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users")
            .WithQueryParameter("page", 1);


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/users?page=1", url);
    }

    [Fact]
    public void BuildFullUrl_WithMultipleQueryParameters_ReturnsCorrectUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users")
            .WithQueryParameter("page", 1)
            .WithQueryParameter("limit", 10)
            .WithQueryParameter("sort", "name");


        var url = reporter.BuildFullUrl();


        Assert.Contains("https://api.example.com/users?", url);
        Assert.Contains("page=1", url);
        Assert.Contains("limit=10", url);
        Assert.Contains("sort=name", url);
        Assert.Contains("&", url);
    }

    [Fact]
    public void BuildFullUrl_WithSpecialCharactersInQueryParameters_ReturnsUrlEncodedUrl()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/search")
            .WithQueryParameter("q", "hello world")
            .WithQueryParameter("filter", "name=John & age>25");


        var url = reporter.BuildFullUrl();

        Assert.Contains("q=hello+world", url);
        Assert.Contains("filter=name%3dJohn+%26+age%3e25", url);
    }

    [Fact]
    public void BuildFullUrl_WithSpecialCharactersInQueryValues_EncodesCorrectly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/search")
            .WithQueryParameter("q", "hello world & more")
            .WithQueryParameter("filter", "name=John & age>25")
            .WithQueryParameter("special", "email@domain.com");


        var url = reporter.BuildFullUrl();


        Assert.Contains("q=hello+world+%26+more", url);
        Assert.Contains("filter=name%3dJohn+%26+age%3e25", url);
        Assert.Contains("special=email%40domain.com", url);
    }

    [Fact]
    public void BuildFullUrl_WithSpecialCharactersInQueryKeys_EncodesCorrectly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/test");

        reporter.QueryParameters["key with spaces"] = "value";
        reporter.QueryParameters["key&with&ampersands"] = "value";


        var url = reporter.BuildFullUrl();


        Assert.Contains("key+with+spaces=value", url);
        Assert.Contains("key%26with%26ampersands=value", url);
    }

    [Fact]
    public void BuildFullUrl_WithUnicodeCharacters_EncodesCorrectly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/search")
            .WithQueryParameter("query", "café naïve résumé")
            .WithQueryParameter("city", "北京");


        var url = reporter.BuildFullUrl();

        Assert.Contains("query=caf%c3%a9+na%c3%afve+r%c3%a9sum%c3%a9", url);
        Assert.Contains("city=%e5%8c%97%e4%ba%ac", url);
    }

    [Fact]
    public void BuildFullUrl_WithEmptyBaseUrl_ReturnsEndpointOnly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("")
            .WithEndpoint("/users");


        var url = reporter.BuildFullUrl();


        Assert.Equal("/users", url);
    }

    [Fact]
    public void BuildFullUrl_WithEmptyEndpoint_ReturnsBaseUrlOnly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("");


        var url = reporter.BuildFullUrl();


        Assert.Equal("https://api.example.com/", url);
    }

    [Fact]
    public void BuildFullUrl_WithBooleanQueryParameter_ReturnsCorrectStringRepresentation()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users")
            .WithQueryParameter("active", true)
            .WithQueryParameter("deleted", false);


        var url = reporter.BuildFullUrl();


        Assert.Contains("active=True", url);
        Assert.Contains("deleted=False", url);
    }

    #endregion

    #region Integration with BaseApiReporter Tests

    [Fact]
    public void RestApiReporter_InheritsFromBaseApiReporter()
    {

        var reporter = new RestApiReporter();


        Assert.IsAssignableFrom<BaseApiReporter<RestApiReporter>>(reporter);
    }

    [Fact]
    public void RestApiReporter_ImplementsIRestApiReporter()
    {

        var reporter = new RestApiReporter();


        Assert.IsAssignableFrom<IRestApiReporter>(reporter);
    }

    [Fact]
    public void RestApiReporter_CanUseBaseApiReporterMethods()
    {

        var reporter = new RestApiReporter();


        reporter
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users")
            .WithTimeout(30)
            .WithHeader("Authorization", "Bearer token")
            .WithJsonBody("{\"test\":\"data\"}")
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("ApiTest");


        Assert.Equal("https://api.example.com", reporter.BaseUrl);
        Assert.Equal("/users", reporter.Endpoint);
        Assert.Equal(30, reporter.TimeoutSeconds);
        Assert.Equal("Bearer token", reporter.Headers["Authorization"]);
        Assert.Equal("{\"test\":\"data\"}", reporter.RequestBody);
        Assert.Equal(ContentType.ApplicationJson, reporter.RequestContentType);
        Assert.Equal(ResultsFormat.Json, reporter.ResultsFormat);
        Assert.Equal("ApiTest", reporter.FileNamePrefix);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void RestApiReporter_HasCorrectDefaultValues()
    {

        var reporter = new RestApiReporter();


        Assert.Equal(ApiHttpMethod.GET, reporter.Method);
        Assert.Empty(reporter.QueryParameters);
        Assert.Empty(reporter.FormData);


        Assert.Equal(string.Empty, reporter.BaseUrl);
        Assert.Equal(string.Empty, reporter.Endpoint);
        Assert.Equal(30, reporter.TimeoutSeconds);
        Assert.True(reporter.FollowRedirects);
        Assert.Equal(Encoding.UTF8, reporter.ContentEncoding);
        Assert.Empty(reporter.Headers);
        Assert.Null(reporter.RequestBody);
        Assert.Null(reporter.RequestContentType);
    }

    #endregion

    #region Fluent API Chaining Tests

    [Fact]
    public void FluentApi_AllMethodsReturnSameInstance()
    {

        var reporter = new RestApiReporter();


        Assert.Same(reporter, reporter.WithMethod(ApiHttpMethod.POST));
        Assert.Same(reporter, reporter.WithQueryParameter("key", "value"));
        Assert.Same(reporter, reporter.WithQueryParameters(new Dictionary<string, object>()));
        Assert.Same(reporter, reporter.WithFormData("field", "value"));
        Assert.Same(reporter, reporter.WithFormData(new Dictionary<string, string>()));
    }

    [Fact]
    public void FluentApi_CanChainMultipleMethods()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/users")
            .WithMethod(ApiHttpMethod.POST)
            .WithQueryParameter("include", "details")
            .WithFormData("name", "John")
            .WithFormData("email", "john@example.com")
            .WithHeader("Accept", "application/json")
            .WithTimeout(60);


        Assert.Equal("https://api.example.com", reporter.BaseUrl);
        Assert.Equal("/users", reporter.Endpoint);
        Assert.Equal(ApiHttpMethod.POST, reporter.Method);
        Assert.Equal("details", reporter.QueryParameters["include"]);
        Assert.Equal("John", reporter.FormData["name"]);
        Assert.Equal("john@example.com", reporter.FormData["email"]);
        Assert.Equal("application/json", reporter.Headers["Accept"]);
        Assert.Equal(60, reporter.TimeoutSeconds);
        Assert.Equal(ContentType.ApplicationFormUrlEncoded, reporter.RequestContentType);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void QueryParameters_CanHandleNullToStringConversion()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/test");


        reporter.QueryParameters["test"] = new object();


        var url = reporter.BuildFullUrl();
        Assert.Contains("test=", url);
    }

    [Fact]
    public void FormData_AndJsonBody_CanCoexist()
    {

        var reporter = new RestApiReporter()
            .WithJsonBody("{\"json\":\"data\"}")
            .WithFormData("form", "data");


        Assert.Equal("{\"json\":\"data\"}", reporter.RequestBody);
        Assert.Equal("data", reporter.FormData["form"]);

        Assert.Equal(ContentType.ApplicationFormUrlEncoded, reporter.RequestContentType);
    }

    [Fact]
    public void BuildFullUrl_WithEmptyQueryParameterValue_HandlesCorrectly()
    {

        var reporter = new RestApiReporter()
            .WithBaseUrl("https://api.example.com")
            .WithEndpoint("/test")
            .WithQueryParameter("empty", "")
            .WithQueryParameter("normal", "value");


        var url = reporter.BuildFullUrl();


        Assert.Contains("empty=", url);
        Assert.Contains("normal=value", url);
    }

    #endregion
}
