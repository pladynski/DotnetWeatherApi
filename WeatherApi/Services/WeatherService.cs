using System.Net.Http.Json;
using WeatherService.Models;

namespace WeatherService;

/// <summary>
/// Graftcode-exposed weather module. Every public method here IS the integration
/// contract (no REST controllers). Methods are <c>static</c> on purpose: this is a
/// stateless facade, so each call is self-contained and the whole result DTO is
/// passed by value in a single round-trip (no server-side object to track).
/// Signatures use only primitives/strings and plain DTOs/arrays, and are synchronous
/// as required by the Graftcode .NET gateway.
/// </summary>
public static class WeatherProvider
{
    private static readonly string? WEATHER_API_URL = Environment.GetEnvironmentVariable("WEATHER_API_URL");
    private static readonly string? WEATHER_API_KEY = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
    private static readonly HttpClient _httpClient = new HttpClient { };

    public static SearchLocation[] SearchLocations(string query, string lang = "en")
    {
        ValidateWeatherApiSettings();

        string apiUrl = $"{WEATHER_API_URL}/search.json?key={WEATHER_API_KEY}&q={query}&lang={lang}";

        HttpResponseMessage response = _httpClient.GetAsync(apiUrl).Result;

        if (response.IsSuccessStatusCode)
        {
            var result = response.Content.ReadFromJsonAsync<SearchLocation[]>().Result;
            return result!;
        }

        throw new HttpRequestException($"Weather API request failed with status code {response.StatusCode}");
    }

    public static Weather GetWeatherForecast(string query, int days = 3, string lang = "en")
    {
        ValidateWeatherApiSettings();

        string apiUrl = $"{WEATHER_API_URL}/forecast.json?key={WEATHER_API_KEY}&q={query}&days={days}&lang={lang}";

        HttpResponseMessage response = _httpClient.GetAsync(apiUrl).Result;

        if (response.IsSuccessStatusCode)
        {
            var result = response.Content.ReadFromJsonAsync<Weather>().Result;
            return result!;
        }

        throw new HttpRequestException($"Weather API request failed with status code {response.StatusCode}");
    }

    private static void ValidateWeatherApiSettings()
    {
        if (string.IsNullOrEmpty(WEATHER_API_URL) || string.IsNullOrEmpty(WEATHER_API_KEY))
        {
            throw new Exception("WEATHER_API_URL and WEATHER_API_KEY must be set in the .env file.");
        }
    }
}
