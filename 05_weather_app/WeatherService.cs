using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _05_weather_app;

class WeatherService
{
    private static readonly HttpClient _http = new();

    public async Task<WeatherData> FetchAsync(double latitude, double longitude)
    {
        string url = $"https://api.open-meteo.com/v1/forecast" +
                     $"?latitude={latitude}&longitude={longitude}" +
                     $"&current=temperature_2m,weathercode,windspeed_10m,relativehumidity_2m" +
                     $"&hourly=temperature_2m,weathercode,windspeed_10m" +
                     $"&timezone=Asia%2FTokyo" +
                     $"&forecast_days=1";

        string json = await _http.GetStringAsync(url);

        var response = JsonSerializer.Deserialize<OpenMeteoResponse>(json)
            ?? throw new Exception("APIレスポンスの解析に失敗しました");

        // 時間別データを組み立てる
        var hourly = response.Hourly;
        var hourlyList = Enumerable.Range(0, hourly.Time.Count)
            .Select(i => new HourlyWeather
            {
                Time      = hourly.Time[i][11..16],   // "2026-05-05T14:00" → "14:00"
                Temp      = hourly.Temperature[i],
                WindSpeed = hourly.WindSpeed[i],
                Icon      = WeatherCodeToIcon(hourly.WeatherCode[i]),
            })
            .ToList();

        return new WeatherData
        {
            Temperature = response.Current.Temperature,
            WindSpeed   = response.Current.WindSpeed,
            Humidity    = response.Current.Humidity,
            Condition   = WeatherCodeToText(response.Current.WeatherCode),
            Icon        = WeatherCodeToIcon(response.Current.WeatherCode),
            Hourly      = hourlyList,
        };
    }

    private static string WeatherCodeToText(int code) => code switch
    {
        0              => "快晴",
        1              => "晴れ",
        2              => "一部曇り",
        3              => "曇り",
        45 or 48       => "霧",
        51 or 53 or 55 => "霧雨",
        61 or 63 or 65 => "雨",
        71 or 73 or 75 => "雪",
        80 or 81 or 82 => "にわか雨",
        95             => "雷雨",
        _              => "不明"
    };

    private static string WeatherCodeToIcon(int code) => code switch
    {
        0                    => "☀",
        1 or 2               => "🌤",
        3                    => "☁",
        45 or 48             => "🌫",
        >= 51 and <= 67      => "🌧",
        >= 71 and <= 77      => "❄",
        >= 80 and <= 82      => "🌦",
        >= 95                => "⛈",
        _                    => "🌡"
    };
}

// --- JSON マッピング ---

class OpenMeteoResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather Current { get; set; } = new();

    [JsonPropertyName("hourly")]
    public HourlyRaw Hourly { get; set; } = new();
}

class CurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("weathercode")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("windspeed_10m")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("relativehumidity_2m")]
    public int Humidity { get; set; }
}

class HourlyRaw
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature { get; set; } = [];

    [JsonPropertyName("weathercode")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("windspeed_10m")]
    public List<double> WindSpeed { get; set; } = [];
}

// --- 画面に渡すデータ ---

class WeatherData
{
    public double Temperature { get; set; }
    public double WindSpeed   { get; set; }
    public int    Humidity    { get; set; }
    public string Condition   { get; set; } = "";
    public string Icon        { get; set; } = "";
    public List<HourlyWeather> Hourly { get; set; } = [];
}

class HourlyWeather
{
    public string Time      { get; set; } = "";
    public double Temp      { get; set; }
    public double WindSpeed { get; set; }
    public string Icon      { get; set; } = "";
    public string Display   => $"{Temp:F1}°C   💨{WindSpeed:F0}km/h";
}
