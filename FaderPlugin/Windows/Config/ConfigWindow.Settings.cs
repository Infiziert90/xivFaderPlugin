using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using faderPlugin.Resources;
using FaderPlugin.Data;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FaderPlugin.Windows.Config;

public partial class ConfigWindow
{
    private List<ConfigEntry> SelectedConfig = [];
    private readonly List<Element> SelectedElements = [];
    private const float AlphaTolerance = 1f / 255f;
    private Constants.OverrideKeys CurrentOverrideKey => (Constants.OverrideKeys)Configuration.OverrideKey;

    private void Settings()
    {
        using var tabItem = ImRaii.TabItem(Language.TabSettings);
        if (!tabItem.Success)
            return;

        #region General Settings

        if (ImGui.CollapsingHeader(Language.SettingsGeneralHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            // Create a 2-column table to align labels and controls.
            // Increase widths so longer text doesn't get cut off.
            using var table = ImRaii.Table("FaderSettingsTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings);
            if (table.Success)
            {
                // Adjust column widths to give more space to labels.
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 300.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthFixed, 220.0f * ImGuiHelpers.GlobalScale);

                //
                // Focus Key
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsFocusKey);
                ImGuiComponents.HelpMarker(Language.SettingsFocusKeyTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                using (var combo = ImRaii.Combo("##UserFocusCombo", CurrentOverrideKey.ToString()))
                {
                    if (combo.Success)
                    {
                        foreach (var option in Enum.GetValues<Constants.OverrideKeys>())
                        {
                            if (ImGui.Selectable(option.ToString(), option.Equals(CurrentOverrideKey)))
                            {
                                Configuration.OverrideKey = (int)option;
                                Configuration.Save();
                            }
                        }
                    }
                }

                //
                // Always User Focus when hotbars are unlocked
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsFocusHotbarUnlock);
                ImGuiComponents.HelpMarker(Language.SettingsFocusHotbarUnlockTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var focusOnHotbarsUnlock = Configuration.FocusOnHotbarsUnlock;
                if (ImGui.Checkbox("##focus_on_unlocked_bars", ref focusOnHotbarsUnlock))
                {
                    Configuration.FocusOnHotbarsUnlock = focusOnHotbarsUnlock;
                    Configuration.Save();
                }

                //
                // Emotes trigger chat activity
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsEmoteActivity);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var emoteChat = Configuration.EmoteActivity;
                if (ImGui.Checkbox("##emote_activity", ref emoteChat))
                {
                    Configuration.EmoteActivity = emoteChat;
                    Configuration.Save();
                }

                //
                // System messages trigger chat activity
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsSystemTrigger);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var importChat = Configuration.ImportantActivity;
                if (ImGui.Checkbox("##important_activity", ref importChat))
                {
                    Configuration.ImportantActivity = importChat;
                    Configuration.Save();
                }

                //
                // Default Delay
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsDelay);
                ImGuiComponents.HelpMarker(Language.SettingsDelayTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var defaultDelayEnabled = Configuration.DefaultDelayEnabled;
                if (ImGui.Checkbox("##default_delay_enabled", ref defaultDelayEnabled))
                {
                    Configuration.DefaultDelayEnabled = defaultDelayEnabled;
                    Configuration.Save();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (defaultDelayEnabled)
                {
                    var idleDelay = (float)TimeSpan.FromMilliseconds(Configuration.DefaultDelay).TotalSeconds;
                    if (ImGui.SliderFloat("##default_delay", ref idleDelay, 0.1f, 15f, $"%.1f {Language.Seconds}"))
                    {
                        Configuration.DefaultDelay = (int)TimeSpan.FromSeconds(Math.Round(idleDelay, 1)).TotalMilliseconds;
                        Configuration.Save();
                    }
                }

                //
                // Chat Activity Timeout
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsChatActivityTimeout);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var chatActivityTimeout = (int)TimeSpan.FromMilliseconds(Configuration.ChatActivityTimeout).TotalSeconds;
                if (ImGui.SliderInt("##chat_activity_timeout", ref chatActivityTimeout, 1, 20, $"%d {Language.Seconds}"))
                {
                    Configuration.ChatActivityTimeout = (int)TimeSpan.FromSeconds(chatActivityTimeout).TotalMilliseconds;
                    Configuration.Save();
                }

                //
                // Enter Transition Time
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsEnterTransition);
                ImGuiComponents.HelpMarker(Language.SettingsEnterTransitionTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var enterTransitionTimeMs = Configuration.EnterTransitionSpeed > AlphaTolerance
                    ? (1.0f / Configuration.EnterTransitionSpeed) * 1000.0f
                    : 1000.0f;
                if (Helper.SliderFloatDiscrete("##enter_transition_time_ms", ref enterTransitionTimeMs, 10.0f, 2000.0f, 10.0f, "{0:0} ms"))
                {
                    Configuration.EnterTransitionSpeed = 1000.0f / enterTransitionTimeMs;
                    Configuration.Save();
                }

                //
                // Exit Transition Time
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsExitTransition);
                ImGuiComponents.HelpMarker(Language.SettingsExitTransitionTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var exitTransitionTimeMs = Configuration.ExitTransitionSpeed > AlphaTolerance
                    ? (1.0f / Configuration.ExitTransitionSpeed) * 1000.0f
                    : 1000.0f;
                if (Helper.SliderFloatDiscrete("##exit_transition_time_ms", ref exitTransitionTimeMs, 10.0f, 2000.0f, 10.0f, "{0:0} ms"))
                {
                    Configuration.ExitTransitionSpeed = 1000.0f / exitTransitionTimeMs;
                    Configuration.Save();
                }

                //
                // Relative Opacity
                //
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsRelativeOpacity);
                ImGuiComponents.HelpMarker(Language.SettingsRelativeOpacityTooltip);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var relativeOpacity = Configuration.RelativeOpacity;
                if (ImGui.Checkbox("##relative_opacity_enabled", ref relativeOpacity))
                {
                    Configuration.RelativeOpacity = relativeOpacity;
                    Configuration.Save();
                }
            }
        }

        // Separator before the element configuration list
        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        Helper.WrappedText(Language.SettingsMultiSelectionHint);
        ImGuiHelpers.ScaledDummy(5);

        #endregion

        // Layout for element selection + config
        var startPos = ImGui.GetCursorPos();
        var style = ImGui.GetStyle();
        var buttonWidth = ImGui.CalcTextSize("Context Action Hotbar   ?").X + style.FramePadding.X * 2 + style.ScrollbarSize;
        var childSize = buttonWidth + style.WindowPadding.X * 2;

        #region Left Child : Element Selection
        using (var child = ImRaii.Child("ElementList", new Vector2(childSize, 0), true))
        {
            if (child.Success)
            {
                foreach (var element in ElementUtil.OrderedElements)
                {
                    if (element.ShouldIgnoreElement())
                        continue;

                    var buttonText = ElementUtil.GetElementName(element);
                    var tooltipText = element.TooltipForElement();
                    if (!string.IsNullOrEmpty(tooltipText))
                        buttonText += "   ?";

                    using var pushedStyle = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));

                    var desiredButtonColor = ImGui.GetColorU32(ImGuiCol.Button);
                    if (SelectedElements.Contains(element))
                        desiredButtonColor = ImGui.GetColorU32(ImGuiColors.HealerGreen);

                    var hasScrollbar = ImGui.GetScrollMaxY() > 0.0f;
                    using var pushedColor = ImRaii.PushColor(ImGuiCol.Button, desiredButtonColor);
                    if (ImGui.Button(buttonText, new Vector2(buttonWidth - (hasScrollbar ? style.ScrollbarSize : 0.0f), 0)))
                    {
                        if (!ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                            SelectedElements.Clear();

                        if (SelectedElements.Count == 0)
                            SelectedConfig = Configuration.GetElementConfig(element);

                        if (!SelectedElements.Remove(element))
                            SelectedElements.Add(element);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        if (!string.IsNullOrEmpty(tooltipText))
                            Helper.Tooltip(tooltipText);

                        var addonNames = ElementUtil.GetAddonName(element);
                        if (addonNames.Length == 0)
                            continue;

                        var color = ImGui.GetColorU32(ImGuiColors.HealerGreen);
                        var drawlist = ImGui.GetBackgroundDrawList();
                        foreach (var addonName in addonNames)
                        {
                            var addonPosition = Addon.GetAddonPosition(addonName);
                            if (!addonPosition.IsPresent)
                                continue;

                            drawlist.AddRect(addonPosition.Start, addonPosition.End, color, 0, ImDrawFlags.None, 5.0f * ImGuiHelpers.GlobalScale);
                        }
                    }
                }
            }
        }
        #endregion

        #region Right Child : Element Configuration
        ImGui.SetCursorPos(startPos with { X = startPos.X + childSize });
        using var contentChild = ImRaii.Child("ConfigPage", Vector2.Zero, true);

        if (!contentChild.Success)
            return;

        // If no elements are selected, do nothing.
        if (SelectedElements.Count == 0)
            return;

        var selectedElement = SelectedElements[0];
        var elementName = ElementUtil.GetElementName(selectedElement);
        if (SelectedElements.Count > 1)
            elementName += $" & {Language.SettingsOthers}";

        ImGui.TextUnformatted(Language.SettingsElementConfiguration.Format(elementName));
        if (SelectedElements.Count > 1)
        {
            if (ImGui.Button(Language.SettingsSyncToElement.Format(selectedElement)))
                SaveSelectedElementsConfig();
        }

        // Draw each condition row
        for (var i = 0; i < SelectedConfig.Count; i++)
        {
            var elementState = SelectedConfig[i].state;

            // State
            var itemWidth = 200.0f * ImGuiHelpers.GlobalScale;
            ImGui.SetNextItemWidth(itemWidth);

            var stateName = StateUtil.GetStateName(elementState);
            if (elementState == State.Default)
            {
                ImGui.NewLine();
                var pos = ImGui.GetCursorPos();
                ImGui.TextUnformatted(stateName);
                ImGui.SetCursorPos(pos with { X = pos.X + itemWidth + ImGui.GetStyle().ItemSpacing.X });
            }
            else
            {
                using (var combo = ImRaii.Combo($"##{elementName}-{i}-state", stateName))
                {
                    if (combo.Success)
                    {
                        foreach (var state in StateUtil.OrderedStates)
                        {
                            if (state is State.None or State.Default)
                                continue;

                            if (ImGui.Selectable(StateUtil.GetStateName(state)))
                            {
                                SelectedConfig[i].state = state;
                                SaveSelectedElementsConfig();
                            }
                        }
                    }
                }

                ImGui.SameLine();
            }

            // Opacity
            var opacity = SelectedConfig[i].Opacity;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.SliderFloat($"##{elementName}-{i}-opacity", ref opacity, 0.0f, 1.0f, $"{Language.Opacity}: %.2f"))
            {
                SelectedConfig[i].Opacity = opacity;
                SaveSelectedElementsConfig();
            }

            // Default Disabled Checkbox
            ImGui.SameLine();
            if (SelectedConfig[i].state == State.Default)
            {
                var isDisabled = Configuration.DisabledElements.TryGetValue(selectedElement, out var disabled) && disabled;
                if (ImGui.Checkbox($"##{elementName}-disabled", ref isDisabled))
                {
                    foreach (var element in SelectedElements)
                    {
                        Configuration.DisabledElements[element] = isDisabled;
                    }
                    SaveSelectedElementsConfig();
                }
                ImGui.SameLine();
                ImGui.TextUnformatted(Language.SettingsDisable);
                ImGuiComponents.HelpMarker(Language.SettingsDisableTooltip);
            }

            // If not default, show reordering & delete buttons
            if (elementState != State.Default)
            {
                ImGui.SameLine();
                using var innerFont = ImRaii.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}##{elementName}-{i}-up"))
                {
                    if (i > 0)
                    {
                        var swap1 = SelectedConfig[i - 1];
                        var swap2 = SelectedConfig[i];
                        if (swap1.state != State.Default && swap2.state != State.Default)
                        {
                            SelectedConfig[i] = swap1;
                            SelectedConfig[i - 1] = swap2;
                            SaveSelectedElementsConfig();
                        }
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button($"{FontAwesomeIcon.ArrowDown.ToIconString()}##{elementName}-{i}-down"))
                {
                    if (i < SelectedConfig.Count - 1)
                    {
                        var swap1 = SelectedConfig[i + 1];
                        var swap2 = SelectedConfig[i];
                        if (swap1.state != State.Default && swap2.state != State.Default)
                        {
                            SelectedConfig[i] = swap1;
                            SelectedConfig[i + 1] = swap2;
                            SaveSelectedElementsConfig();
                        }
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button($"{FontAwesomeIcon.TrashAlt.ToIconString()}##{elementName}-{i}-delete"))
                {
                    SelectedConfig.RemoveAt(i);
                    SaveSelectedElementsConfig();
                }
            }
        }

        // Add new condition row
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##{elementName}-add"))
            {
                SelectedConfig.Add(new ConfigEntry(State.None, Setting.Show));
                var swap1 = SelectedConfig[^1];
                var swap2 = SelectedConfig[^2];
                SelectedConfig[^2] = swap1;
                SelectedConfig[^1] = swap2;
                SaveSelectedElementsConfig();
            }
        }

        // Warning Label
        var defaultDisabled = Configuration.DisabledElements.TryGetValue(selectedElement, out var isElementDisabled) && isElementDisabled;
        var hoverPresent = SelectedConfig.Any(e => e.state == State.Hover);

        if (defaultDisabled && hoverPresent)
        {
            Helper.TextColored(ImGuiColors.DalamudRed, Language.StateWarning);
        }
        else
        {   // spacing & prevents Layout shift when the warning appears
            ImGui.NewLine();
        }
        // Fade Setting Overrides
        using var overrideTable = ImRaii.Table("FadeOverrideTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings);
        if (overrideTable.Success)
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Controls", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.SettingsFadeOverride);
            ImGui.SameLine();
            var useOverride = Configuration.FadeOverrides[selectedElement].UseCustomFadeTimes;
            if (ImGui.Checkbox($"##{elementName}-fadeOverride", ref useOverride))
            {
                Configuration.FadeOverrides[selectedElement].UseCustomFadeTimes = useOverride;
                Configuration.Save();
            }
            ImGui.TableNextColumn();
            if (useOverride)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsEnterTransition);
                ImGuiComponents.HelpMarker(Language.SettingsEnterTransitionTooltip);
                ImGui.TableNextColumn();

                var itemWidth = 200.0f * ImGuiHelpers.GlobalScale;
                ImGui.SetNextItemWidth(itemWidth);
                var fadeInTime = Configuration.FadeOverrides[selectedElement].EnterTransitionSpeedOverride > AlphaTolerance
                    ? (1.0f / Configuration.FadeOverrides[selectedElement].EnterTransitionSpeedOverride) * 1000.0f
                    : 1000.0f;
                if (Helper.SliderFloatDiscrete($"##{elementName}-fadeIn", ref fadeInTime, 10.0f, 2000.0f, 10.0f, "{0:0} ms"))
                {
                    Configuration.FadeOverrides[selectedElement].EnterTransitionSpeedOverride = 1000.0f / fadeInTime;
                    Configuration.Save();
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.SettingsExitTransition);
                ImGuiComponents.HelpMarker(Language.SettingsExitTransitionTooltip);
                ImGui.TableNextColumn();

                ImGui.SetNextItemWidth(itemWidth);
                var fadeOutTime = Configuration.FadeOverrides[selectedElement].ExitTransitionSpeedOverride > AlphaTolerance
                    ? (1.0f / Configuration.FadeOverrides[selectedElement].ExitTransitionSpeedOverride) * 1000.0f
                    : 1000.0f;
                if (Helper.SliderFloatDiscrete($"##{elementName}-fadeOut", ref fadeOutTime, 10.0f, 2000.0f, 10.0f, "{0:0} ms"))
                {
                    Configuration.FadeOverrides[selectedElement].ExitTransitionSpeedOverride = 1000.0f / fadeOutTime;
                    Configuration.Save();
                }
            }
        }
    }

    #endregion

    private void SaveSelectedElementsConfig()
    {
        foreach (var element in SelectedElements)
        {
            Configuration.elementsConfig[element] = SelectedConfig
                .Select(entry => new ConfigEntry(entry.state, entry.setting) { Opacity = entry.Opacity })
                .ToList();

            var elementDisabled = Configuration.DisabledElements.TryGetValue(SelectedElements[0], out var disabled) && disabled;
            Configuration.DisabledElements[element] = elementDisabled;
        }

        Configuration.Save();
    }
}
