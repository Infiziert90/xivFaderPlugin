using System;
using Dalamud.Plugin;

namespace FaderPlugin.Integrations;

public sealed class MagitekStratagemIntegration : IDisposable
{
    private const string DataKey = "MagitekStratagemPlugin.TrackerData";

    private float[]? TrackerData;

    public MagitekStratagemIntegration()
    {
    }

    public void Dispose()
    {
        Plugin.PluginInterface.RelinquishData(DataKey);
    }

    public float[]? GetTrackerData()
    {
        if (TrackerData == null)
        {
            TrackerData = Plugin.PluginInterface.GetData<float[]>(DataKey);
        }

        return TrackerData;
    }
}
