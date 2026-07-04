namespace FitnessQuest.Models;

/// <summary>
/// One sample from an imported workout track (TCX/GPX). Serialized as JSON
/// into <see cref="CardioSession.TrackJson"/>.
/// </summary>
public class TrackPoint
{
    /// <summary>Seconds since the start of the session.</summary>
    public double T { get; set; }

    /// <summary>Cumulative distance in km at this point.</summary>
    public double DistanceKm { get; set; }

    /// <summary>Heart rate in bpm (0 if unknown).</summary>
    public int Hr { get; set; }

    /// <summary>Altitude in metres (0 if unknown).</summary>
    public double Ele { get; set; }
}
