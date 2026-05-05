using System.Windows;
using System.Windows.Controls;

namespace _05_weather_app;

public partial class MainWindow : Window
{
    private readonly WeatherService _service = new();

    private readonly (string Name, double Lat, double Lon)[] _cities =
    [
        ("松戸市",   35.7877, 139.9025),
        ("柏市",     35.8687, 139.9692),
        ("流山市",   35.8556, 139.9046),
        ("野田市",   35.9609, 139.8757),
        ("我孫子市", 35.8620, 140.0226),
        ("成田市",   35.7769, 140.3184),
        ("銚子市",   35.7344, 140.8268),
        ("旭市",     35.7103, 140.5942),
        ("千葉市",   35.6072, 140.1063),
        ("船橋市",   35.6944, 139.9828),
        ("市川市",   35.7219, 139.9311),
        ("習志野市", 35.6839, 140.0228),
        ("八千代市", 35.7229, 140.0997),
        ("佐倉市",   35.7149, 140.2236),
        ("四街道市", 35.6673, 140.1693),
        ("市原市",   35.4979, 140.1162),
        ("浦安市",   35.6536, 139.9040),
        ("木更津市", 35.3748, 139.9229),
        ("富津市",   35.2975, 139.8673),
        ("館山市",   34.9957, 139.8696),
        ("鴨川市",   35.1132, 140.0986),
        ("勝浦市",   35.1536, 140.3159),
        ("いすみ市", 35.2563, 140.3648),
    ];

    public MainWindow()
    {
        InitializeComponent();
        foreach (var city in _cities)
            CityBox.Items.Add(city.Name);
        CityBox.SelectedIndex = 0;
    }

    private async void CityBox_SelectionChanged(object _, SelectionChangedEventArgs _2)
        => await LoadWeatherAsync();

    private async void RefreshButton_Click(object _, RoutedEventArgs _2)
        => await LoadWeatherAsync();

    private async Task LoadWeatherAsync()
    {
        int index = CityBox.SelectedIndex;
        if (index < 0) return;

        ConditionText.Text = "読み込み中...";
        TempText.Text = "--°C";
        IconText.Text = "🌡";
        HourlyList.ItemsSource = null;

        try
        {
            var (_, lat, lon) = _cities[index];  // 分解代入: Name は不要なので _ で捨てる
            WeatherData data = await _service.FetchAsync(lat, lon);

            IconText.Text      = data.Icon;
            TempText.Text      = $"{data.Temperature:F1}°C";
            ConditionText.Text = data.Condition;
            HumidityText.Text  = $"{data.Humidity}%";
            WindText.Text      = $"{data.WindSpeed:F1} km/h";
            HourlyList.ItemsSource = data.Hourly;
        }
        catch (Exception ex)
        {
            ConditionText.Text = $"エラー: {ex.Message}";
        }
    }
}
