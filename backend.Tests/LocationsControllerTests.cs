using System.Net;
using System.Text;
using Backend.Features.Locations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace backend.Tests;

public sealed class LocationsControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public async Task SearchAsync_ShortOrEmptyQuery_ReturnsEmptyList(string? q)
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.OK, "[]");

        var result = await controller.SearchAsync(q, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LocationSearchResponse>(ok.Value);
        Assert.Empty(body.Suggestions);
    }

    [Fact]
    public async Task SearchAsync_UpstreamFailure_Returns503Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.ServiceUnavailable, "");

        var result = await controller.SearchAsync("Budapest", ct);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, obj.StatusCode);
        Assert.IsType<ProblemDetails>(obj.Value);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsMappedSuggestions()
    {
        var ct = TestContext.Current.CancellationToken;
        const string json =
            """[{"place_id":123,"display_name":"Budapest, Hungary","lat":"47.4979","lon":"19.0402"}]""";
        var controller = CreateController(HttpStatusCode.OK, json);

        var result = await controller.SearchAsync("Budapest", ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LocationSearchResponse>(ok.Value);
        var suggestion = Assert.Single(body.Suggestions);
        Assert.Equal("123", suggestion.Id);
        Assert.Equal("Budapest, Hungary", suggestion.LocationText);
        Assert.Equal(47.4979, suggestion.Latitude, precision: 4);
        Assert.Equal(19.0402, suggestion.Longitude, precision: 4);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("hu", "hu")]
    [InlineData("hu-HU,hu;q=0.9,en;q=0.8", "hu")]
    [InlineData("en-GB,en;q=0.9,hu;q=0.8", "en")]
    [InlineData("de-DE,de;q=0.9", "en")]
    public async Task SearchAsync_SendsUiLanguageToNominatim(string? acceptLanguage, string expectedLanguage)
    {
        var ct = TestContext.Current.CancellationToken;
        var requestedUris = new List<Uri>();
        var controller = CreateController(
            HttpStatusCode.OK,
            "[]",
            acceptLanguage: acceptLanguage,
            requestedUris: requestedUris);

        await controller.SearchAsync("Budapest", ct);

        var requestUri = Assert.Single(requestedUris);
        Assert.Equal(expectedLanguage, GetQueryValue(requestUri, "accept-language"));
    }

    [Fact]
    public async Task SearchAsync_CachesResultsPerLanguage()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestedUris = new List<Uri>();

        await CreateController(HttpStatusCode.OK, "[]", "hu", cache, requestedUris).SearchAsync("Budapest", ct);
        await CreateController(HttpStatusCode.OK, "[]", "en", cache, requestedUris).SearchAsync("Budapest", ct);
        await CreateController(HttpStatusCode.OK, "[]", "hu-HU", cache, requestedUris).SearchAsync("budapest", ct);

        Assert.Equal(
            new string?[] { "hu", "en" },
            requestedUris.Select(uri => GetQueryValue(uri, "accept-language")).ToArray());
    }

    [Theory]
    [InlineData(91.0, 0.0)]
    [InlineData(-91.0, 0.0)]
    public async Task ReverseAsync_InvalidLatitude_ReturnsValidationProblemWithLatError(
        double lat, double lon)
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.OK, "{}");

        var result = await controller.ReverseAsync(lat, lon, ct);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("lat"));
        Assert.DoesNotContain("lon", problem.Errors.Keys);
    }

    [Theory]
    [InlineData(0.0, 181.0)]
    [InlineData(0.0, -181.0)]
    public async Task ReverseAsync_InvalidLongitude_ReturnsValidationProblemWithLonError(
        double lat, double lon)
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.OK, "{}");

        var result = await controller.ReverseAsync(lat, lon, ct);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("lon"));
        Assert.DoesNotContain("lat", problem.Errors.Keys);
    }

    [Fact]
    public async Task ReverseAsync_BothInvalid_ReturnsValidationProblemWithBothErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.OK, "{}");

        var result = await controller.ReverseAsync(200.0, 200.0, ct);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("lat"));
        Assert.True(problem.Errors.ContainsKey("lon"));
    }

    [Fact]
    public async Task ReverseAsync_UpstreamFailure_Returns503Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.ServiceUnavailable, "");

        var result = await controller.ReverseAsync(47.0, 19.0, ct);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, obj.StatusCode);
        Assert.IsType<ProblemDetails>(obj.Value);
    }

    [Fact]
    public async Task ReverseAsync_ValidCoordinates_ReturnsMappedLocation()
    {
        var ct = TestContext.Current.CancellationToken;
        const string json =
            """{"place_id":456,"display_name":"Some Street, Budapest","lat":"47.4979","lon":"19.0402"}""";
        var controller = CreateController(HttpStatusCode.OK, json);

        var result = await controller.ReverseAsync(47.4979, 19.0402, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LocationReverseResponse>(ok.Value);
        Assert.Equal("456", body.Location.Id);
        Assert.Equal("Some Street, Budapest", body.Location.LocationText);
    }

    [Fact]
    public async Task ReverseAsync_NominatimReturnsEmptyBody_FallsBackToInputCoordinates()
    {
        var ct = TestContext.Current.CancellationToken;
        var controller = CreateController(HttpStatusCode.OK, "{}");

        var result = await controller.ReverseAsync(47.0, 19.0, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LocationReverseResponse>(ok.Value);
        Assert.Equal("47, 19", body.Location.LocationText);
        Assert.Equal(47.0, body.Location.Latitude);
        Assert.Equal(19.0, body.Location.Longitude);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("hu", "hu")]
    [InlineData("en-GB", "en")]
    public async Task ReverseAsync_SendsUiLanguageToNominatim(string? acceptLanguage, string expectedLanguage)
    {
        var ct = TestContext.Current.CancellationToken;
        var requestedUris = new List<Uri>();
        var controller = CreateController(
            HttpStatusCode.OK,
            "{}",
            acceptLanguage: acceptLanguage,
            requestedUris: requestedUris);

        await controller.ReverseAsync(47.4979, 19.0402, ct);

        var requestUri = Assert.Single(requestedUris);
        Assert.Equal(expectedLanguage, GetQueryValue(requestUri, "accept-language"));
    }

    [Fact]
    public async Task ReverseAsync_CachesResultsPerLanguage()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestedUris = new List<Uri>();

        await CreateController(HttpStatusCode.OK, "{}", "hu", cache, requestedUris).ReverseAsync(47.4979, 19.0402, ct);
        await CreateController(HttpStatusCode.OK, "{}", "en", cache, requestedUris).ReverseAsync(47.4979, 19.0402, ct);
        await CreateController(HttpStatusCode.OK, "{}", "hu", cache, requestedUris).ReverseAsync(47.4979, 19.0402, ct);

        Assert.Equal(
            new string?[] { "hu", "en" },
            requestedUris.Select(uri => GetQueryValue(uri, "accept-language")).ToArray());
    }

    private static LocationsController CreateController(
        HttpStatusCode statusCode,
        string responseBody,
        string? acceptLanguage = null,
        IMemoryCache? cache = null,
        List<Uri>? requestedUris = null)
    {
        var httpContext = new DefaultHttpContext();
        if (acceptLanguage is not null)
        {
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
        }

        var controller = new LocationsController(
            new FakeHttpClientFactory(statusCode, responseBody, requestedUris),
            new ConfigurationBuilder().Build(),
            cache ?? new MemoryCache(new MemoryCacheOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new FakeProblemDetailsFactory()
        };
        return controller;
    }

    private static string? GetQueryValue(Uri uri, string key)
        => QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var value) ? value.ToString() : null;

    private sealed class FakeHttpClientFactory(HttpStatusCode statusCode, string body, List<Uri>? requestedUris)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new FakeMessageHandler(statusCode, body, requestedUris))
            {
                BaseAddress = new Uri("https://nominatim.openstreetmap.org/")
            };
    }

    private sealed class FakeMessageHandler(HttpStatusCode statusCode, string body, List<Uri>? requestedUris)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
            {
                requestedUris?.Add(request.RequestUri);
            }

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FakeProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new() { Status = statusCode, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary) { Status = statusCode ?? 400 };
    }
}
