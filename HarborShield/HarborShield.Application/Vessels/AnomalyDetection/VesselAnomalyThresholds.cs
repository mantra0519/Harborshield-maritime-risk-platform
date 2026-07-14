namespace HarborShield.Application.Vessels.AnomalyDetection;

public static class VesselAnomalyThresholds
{
    /// <summary>Generous upper bound for any cargo vessel; higher reported speeds suggest bad/spoofed data.</summary>
    public const double MaxPlausibleSpeedKnots = 40;

    /// <summary>How much faster the implied speed (from distance/time) can be than the reported speed before it's suspicious.</summary>
    public const double ImpliedSpeedMultiplierThreshold = 2.5;

    /// <summary>Below this, GPS jitter alone can produce an inflated implied speed - ignore.</summary>
    public const double MinDistanceForSpeedCheckKm = 1.0;

    /// <summary>Below this, a high implied speed is not worth flagging even if it exceeds the reported speed.</summary>
    public const double MinImpliedSpeedKnotsToFlag = 5.0;

    public static readonly TimeSpan TrackingGapThreshold = TimeSpan.FromHours(4);
}
