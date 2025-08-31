using System;

namespace FaderPlugin.Animation;

/// <summary>
/// Delegate for easing curves.
/// </summary>
/// <param name="progress">
/// A value in [0,1] representing how far along the interpolation is.
/// </param>
/// <returns>
/// The eased progress in [0,1].
/// </returns>
internal delegate float EasingFunc(float progress);

/// <summary>
/// Collection of easing functions for interpolation.
/// </summary>
internal static class Easing
{
    /// <summary>
    /// Linear easing (constant speed)
    /// </summary>
    public static float Linear(float progress) => progress;

    // TODO: add specific Easing Functions or implement cubic bezier (didn't bother since linear works really well for opacity)
}

internal class Tween(float start, float end, long startTime, long duration, EasingFunc easing)
{
    public float StartValue { get; } = start;
    public float EndValue { get; } = end;
    public long StartTime { get; } = startTime;
    public long Duration { get; } = duration;
    private readonly EasingFunc Easing = easing;

    /// <summary>
    /// Computes the interpolated value at the given time.
    /// </summary>
    public float Value(long now)
    {
        if (Duration <= 0)
            return EndValue;

        var elapsedTime = now - StartTime;
        var rawProgress = elapsedTime / (float)Duration;
        var normalizedProgress = Math.Clamp(rawProgress, 0f, 1f);

        return StartValue + (EndValue - StartValue) * Easing(normalizedProgress);
    }

    /// <summary>
    /// Indicates whether the tween has completed.
    /// </summary>
    public bool IsComplete(long now) => now >= StartTime + Duration;
}
