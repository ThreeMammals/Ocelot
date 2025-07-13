namespace Ocelot.Samples.Metadata.Models;

public class WeatherCurrent
{
    public long? last_updated_epoch { get; set; }
    public string? last_updated { get; set; }
    public float? temp_c { get; set; }
    public float? temp_f { get; set; }
    public int? is_day { get; set; }
    public WeatherCurrentCondition? condition { get; set; }
    public float? wind_mph { get; set; }
    public float? wind_kph { get; set; }
    public int? wind_degree { get; set; }
    public string? wind_dir { get; set; }
    public float? pressure_mb { get; set; }
    public float? pressure_in { get; set; }
    public float? precip_mm { get; set; }
    public float? precip_in { get; set; }
    public int? humidity { get; set; }
    public int? cloud { get; set; }
    public float? feelslike_c { get; set; }
    public float? feelslike_f { get; set; }
    public float? windchill_c { get; set; }
    public float? windchill_f { get; set; }
    public float? heatindex_c { get; set; }
    public float? heatindex_f { get; set; }
    public float? dewpoint_c { get; set; }
    public float? dewpoint_f { get; set; }
    public float? vis_km { get; set; }
    public float? vis_miles { get; set; }
    public float? uv { get; set; }
    public float? gust_mph { get; set; }
    public float? gust_kph { get; set; }
}
