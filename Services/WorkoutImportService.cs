using System.Globalization;
using System.Xml.Linq;
using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>Result of parsing a TCX/GPX file into a cardio session.</summary>
public class ImportedWorkout
{
    public CardioType Type { get; set; } = CardioType.Running;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public double DistanceKm { get; set; }
    public double DurationMinutes { get; set; }
    public int AvgHeartRate { get; set; }
    public int MaxHeartRate { get; set; }
    public double ElevationGainM { get; set; }
    public List<TrackPoint> Points { get; set; } = new();
}

/// <summary>
/// Parses exported workout files (TCX and GPX, e.g. from Mobvoi Health) into a
/// <see cref="ImportedWorkout"/> with a per-point heart-rate / distance /
/// altitude track.
/// </summary>
public class WorkoutImportService
{
    public ImportedWorkout? Parse(string content)
    {
        content = content.TrimStart('﻿', ' ', '\r', '\n', '\t');
        try
        {
            var doc = XDocument.Parse(content);
            var root = doc.Root?.Name.LocalName ?? string.Empty;
            if (root.Equals("TrainingCenterDatabase", StringComparison.OrdinalIgnoreCase))
                return ParseTcx(doc);
            if (root.Equals("gpx", StringComparison.OrdinalIgnoreCase))
                return ParseGpx(doc);
            return null;
        }
        catch
        {
            return null;
        }
    }

    // ---- TCX ----
    private static ImportedWorkout? ParseTcx(XDocument doc)
    {
        // Local-name matching avoids namespace headaches across TCX variants.
        var activity = Descendants(doc, "Activity").FirstOrDefault();
        if (activity is null) return null;

        var result = new ImportedWorkout
        {
            Type = MapSport(activity.Attribute("Sport")?.Value)
        };

        var raw = new List<(DateTime time, double dist, int hr, double? ele)>();
        foreach (var tp in Descendants(activity, "Trackpoint"))
        {
            var timeStr = Local(tp, "Time")?.Value;
            if (!DateTimeOffset.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time))
                continue;

            double dist = ParseD(Local(tp, "DistanceMeters")?.Value);
            int hr = (int)ParseD(Local(Local(tp, "HeartRateBpm"), "Value")?.Value);
            double? ele = Local(tp, "AltitudeMeters") is { } a ? ParseD(a.Value) : null;
            raw.Add((time.UtcDateTime, dist, hr, ele));
        }
        return Build(result, raw);
    }

    // ---- GPX ----
    private static ImportedWorkout? ParseGpx(XDocument doc)
    {
        var result = new ImportedWorkout();
        var raw = new List<(DateTime time, double dist, int hr, double? ele)>();

        double cumDist = 0;
        double? prevLat = null, prevLon = null;
        foreach (var pt in Descendants(doc, "trkpt"))
        {
            if (!double.TryParse(pt.Attribute("lat")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(pt.Attribute("lon")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                continue;

            var timeStr = Local(pt, "time")?.Value;
            if (!DateTimeOffset.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time))
                continue;

            if (prevLat is double pla && prevLon is double plo)
                cumDist += Haversine(pla, plo, lat, lon);
            prevLat = lat; prevLon = lon;

            double? ele = Local(pt, "ele") is { } e ? ParseD(e.Value) : null;
            // HR lives in extensions (gpxtpx:hr or hr), matched by local name.
            int hr = (int)ParseD(Descendants(pt, "hr").FirstOrDefault()?.Value);

            raw.Add((time.UtcDateTime, cumDist, hr, ele));
        }
        return Build(result, raw);
    }

    private static ImportedWorkout? Build(ImportedWorkout result, List<(DateTime time, double dist, int hr, double? ele)> raw)
    {
        if (raw.Count < 2) return null;
        raw.Sort((a, b) => a.time.CompareTo(b.time));

        var start = raw[0].time;
        result.StartedAt = start.ToLocalTime();

        double elevGain = 0;
        double? lastEle = null;
        var hrs = new List<int>();

        foreach (var r in raw)
        {
            result.Points.Add(new TrackPoint
            {
                T = (r.time - start).TotalSeconds,
                DistanceKm = r.dist / 1000.0,
                Hr = r.hr,
                Ele = r.ele ?? 0
            });
            if (r.hr > 0) hrs.Add(r.hr);
            if (r.ele is double e)
            {
                if (lastEle is double le && e > le) elevGain += e - le;
                lastEle = e;
            }
        }

        result.DistanceKm = result.Points[^1].DistanceKm;
        result.DurationMinutes = result.Points[^1].T / 60.0;
        result.AvgHeartRate = hrs.Count > 0 ? (int)Math.Round(hrs.Average()) : 0;
        result.MaxHeartRate = hrs.Count > 0 ? hrs.Max() : 0;
        result.ElevationGainM = Math.Round(elevGain);
        return result;
    }

    // ---- helpers ----
    private static CardioType MapSport(string? sport) => sport?.ToLowerInvariant() switch
    {
        "biking" or "cycling" => CardioType.Cycling,
        "walking" or "hiking" => CardioType.Walking,
        _ => CardioType.Running
    };

    private static IEnumerable<XElement> Descendants(XContainer c, string localName) =>
        c.Descendants().Where(e => e.Name.LocalName == localName);

    private static XElement? Local(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static double ParseD(string? s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // metres
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
