using System;
using Dalamud.Plugin;

namespace FaderPlugin.Integrations;

public sealed class MagitekStratagemIntegration : IDisposable
{
    private readonly IDalamudPluginInterface PluginInterface;
    private const string DataKey = "MagitekStratagemPlugin.TrackerData";

    private float[]? TrackerData;

    public MagitekStratagemIntegration(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Dispose()
    {
        PluginInterface.RelinquishData(DataKey);
    }

    public float[]? GetTrackerData()
    {
        if (TrackerData == null)
        {
            TrackerData = PluginInterface.GetData<float[]>(DataKey);
        }

        return TrackerData;
    }
}
