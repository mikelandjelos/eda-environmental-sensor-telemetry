namespace EventInfo.Models;

public class SensorEvent
{
    public DateTime Timestamp { get; set; }
    public SensorEventType Type { get; set; }
    public string? Device { get; set; }
    public string? Measurement { get; set; }
    public string? Message { get; set; }
    public StatisticData? StatisticData { get; set; }
}

public class StatisticData
{
    public double CurrentValue { get; set; }
    public double DeviationNominal { get; set; }
    public double DeviationPercent { get; set; }
    public double RunningAverage { get; set; }
    public int HistoryLength { get; set; }
}

public enum SensorEventType
{
    NUMERIC,
    BINARY
}
