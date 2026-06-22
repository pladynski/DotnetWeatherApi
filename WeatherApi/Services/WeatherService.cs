using System.Net.Http.Json;
using WeatherService.Models;

namespace WeatherService;

/// <summary>
/// Graftcode-exposed weather module. Every public method here IS the integration
/// contract (no REST controllers). Methods are <c>static</c> on purpose: this is a
/// stateless facade, so each call is self-contained and the whole result DTO is
/// passed by value in a single round-trip (no server-side object to track).
/// Signatures use only primitives/strings and plain DTOs, and are synchronous as
/// required by the Graftcode .NET gateway.
/// </summary>
public static class WeatherProvider
{
    private static readonly string? WEATHER_API_URL = Environment.GetEnvironmentVariable("WEATHER_API_URL");
    private static readonly string? WEATHER_API_KEY = Environment.GetEnvironmentVariable("WEATHER_API_KEY");

    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private const int MaxRetries = 3;

    public static SearchLocation[] SearchLocations(string query, string lang = "en")
    {
        ValidateWeatherApiSettings();

        string apiUrl = $"{WEATHER_API_URL}/search.json?key={WEATHER_API_KEY}&q={query}&lang={lang}";

        var result = GetWithRetry<SearchLocation[]>(apiUrl);
        return result ?? Array.Empty<SearchLocation>();
    }

    public static Weather GetWeatherForecast(string query, int days = 3, string lang = "en")
    {
        ValidateWeatherApiSettings();

        string apiUrl = $"{WEATHER_API_URL}/forecast.json?key={WEATHER_API_KEY}&q={query}&days={days}&lang={lang}";

        var result = GetWithRetry<Weather>(apiUrl);
        return result ?? new Weather();
    }

    private static T? GetWithRetry<T>(string apiUrl)
    {
        Exception? lastError = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                HttpResponseMessage response = _httpClient.GetAsync(apiUrl).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult();
                }

                // Upstream provider is flaky (e.g. intermittent 502) -> retry on 5xx.
                if ((int)response.StatusCode < 500)
                {
                    throw new Exception(
                        $"Weather API request failed with status code {(int)response.StatusCode} ({response.StatusCode}).");
                }

                lastError = new Exception(
                    $"Weather API returned {(int)response.StatusCode} ({response.StatusCode}).");
            }
            catch (TaskCanceledException ex)
            {
                lastError = new Exception("Weather API request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                lastError = new Exception("Weather API request failed.", ex);
            }

            if (attempt < MaxRetries)
            {
                // Exponential backoff: 200ms, 400ms, ...
                Thread.Sleep(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
            }
        }

        throw lastError ?? new Exception("Weather API request failed after retries.");
    }

    private static void ValidateWeatherApiSettings()
    {
        if (string.IsNullOrEmpty(WEATHER_API_URL) || string.IsNullOrEmpty(WEATHER_API_KEY))
        {
            throw new Exception(
                "WEATHER_API_URL and WEATHER_API_KEY environment variables must be set.");
        }
    }
}
