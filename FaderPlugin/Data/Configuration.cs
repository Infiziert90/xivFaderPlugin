using Dalamud.Configuration;
using faderPlugin.Data;
using System;
using System.Collections.Generic;

namespace FaderPlugin.Data;

public class ConfigEntry
{
    public State state { get; set; }
    public Setting setting { get; set; }
    public float Opacity { get; set; } = 1.0f;

    public ConfigEntry(State state, Setting setting)
    {
        this.state = state;
        this.setting = setting;
    }
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public event Action? OnSave;

    public int Version { get; set; } = 6;
    public Dictionary<Element, List<ConfigEntry>> elementsConfig { get; set; } = [];
    public Dictionary<Element, bool> DisabledElements { get; set; } = [];
    public Dictionary<Element, FadeOverride> FadeOverrides { get; set; } = [];
    public List<HoverGroup> HoverGroups { get; set; } = [];
    public bool DefaultDelayEnabled { get; set; } = true;
    public int DefaultDelay { get; set; } = 2000;
    public int ChatActivityTimeout { get; set; } = 5 * 1000;
    public int OverrideKey { get; set; } = 0x12;
    public bool FocusOnHotbarsUnlock { get; set; }
    public bool EmoteActivity { get; set; }
    public bool ImportantActivity { get; set; }
    public float DefaultAlpha { get; set; } = 1.0f; // 1.0f = fully opaque, 0.0f = fully transparent
    public float EnterTransitionSpeed { get; set; } = 4.0f; // alpha change per frame when fading in (4.0f = 250ms for full transition)
    public float ExitTransitionSpeed { get; set; } = 1.0f; // alpha change per frame when fading out (1.0f = 1 second for full transition)
    public bool RelativeOpacity { get; set; } = false;

    public void Initialize()
    {
        // Initialise the config.
        elementsConfig ??= [];
        FadeOverrides ??= [];
        DisabledElements ??= [];
        foreach (var element in Enum.GetValues<Element>())
        {
            if (!elementsConfig.ContainsKey(element))
                elementsConfig[element] = [new ConfigEntry(State.Default, Setting.Show)];
            if (!FadeOverrides.ContainsKey(element))
                FadeOverrides[element] = new FadeOverride();
            if (!DisabledElements.ContainsKey(element))
                DisabledElements[element] = false;
        }
        FixLegacyConfig();
        Save();
    }

    public void FixLegacyConfig()
    {
        foreach (var kvp in elementsConfig)
        {
            var element = kvp.Key;
            var entries = kvp.Value;

            foreach (var entry in entries)
            {
                // If the entry is set to Hide
                // then update the opacity to 0 to keep the configuration working as before.
                // also set it as disabled, so that old behaviour is used.
                if (entry is { setting: Setting.Hide })
                {
                    DisabledElements[element] = true;

                    // adjust opacity once and set all settings to show so that this won't trigger again
                    entry.Opacity = 0;
                    entry.setting = Setting.Show;
                }
            }
        }
    }

    public List<ConfigEntry> GetElementConfig(Element elementId)
    {
        if (!elementsConfig.ContainsKey(elementId))
            elementsConfig[elementId] = [];

        return elementsConfig[elementId];
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
        OnSave?.Invoke();
    }
}