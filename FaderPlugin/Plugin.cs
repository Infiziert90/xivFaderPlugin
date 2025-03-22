using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using faderPlugin.Resources;
using FaderPlugin.Data;
using FaderPlugin.Windows.Config;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace FaderPlugin;

public class Plugin : IDalamudPlugin
{
    // Plugin services
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static IKeyState KeyState { get; set; } = null!;
    [PluginService] public static IFramework Framework { get; set; } = null!;
    [PluginService] public static IClientState ClientState { get; set; } = null!;
    [PluginService] public static ICondition Condition { get; set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; set; } = null!;
    [PluginService] public static IGameGui GameGui { get; set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IGamepadState GamepadState { get; private set; } = null!;
    
    // Configuration and windows.
    public readonly Configuration Config;
    private readonly WindowSystem WindowSystem = new("Fader");
    private readonly ConfigWindow ConfigWindow;

    // State maps and timers.
    private readonly Dictionary<State, bool> StateMap = [];
    private bool StateChanged;
    private long LastChatActivity = Environment.TickCount64;
    private readonly Dictionary<string, bool> AddonHoverStates = [];
    private HashSet<string> PreviousHoveredAddons = [];
    private readonly Dictionary<string, Element> AddonNameToElement = [];
    private bool ConfigChanged;


    // Opacity Management
    private readonly Dictionary<string, float> CurrentAlphas = [];
    private readonly Dictionary<string, float> TargetAlphas = [];
    private readonly Dictionary<string, bool> FinishingHover = [];

    // Commands
    private const string CommandName = "/pfader";
    private bool Enabled = true;

    // Territory Excel sheet.
    private readonly ExcelSheet<TerritoryType> TerritorySheet;

    // Delay management Utility
    private readonly Dictionary<string, long> DelayTimers = [];
    private readonly Dictionary<string, ConfigEntry> LastNonDefaultEntry = [];

    // Enum Cache
    private static readonly Element[] AllElements = Enum.GetValues<Element>();
    private static readonly State[] AllStates = Enum.GetValues<State>();

    public Plugin()
    {
        LoadConfig(out Config);
        LanguageChanged(PluginInterface.UiLanguage);

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        TerritorySheet = Data.GetExcelSheet<TerritoryType>();

        Framework.Update += OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;

        CommandManager.AddHandler(CommandName, new CommandInfo(FaderCommandHandler)
        {
            HelpMessage = "Opens settings\n't' toggles whether it's enabled.\n'on' enables the plugin\n'off' disables the plugin."
        });

        foreach (var state in AllStates)
            StateMap[state] = state == State.Default;

        foreach (var element in AllElements)
        {
            if (element.ShouldIgnoreElement())
                continue;

            var addonNames = ElementUtil.GetAddonName(element);
            foreach (var addonName in addonNames)
            {
                AddonNameToElement.TryAdd(addonName, element);
            }
        }

        ChatGui.ChatMessage += OnChatMessage;
        PluginInterface.LanguageChanged += LanguageChanged;
        Config.OnSave += OnConfigChanged;

        // Recover from previous misconfiguration
        if (Config.DefaultDelay == 0)
            Config.DefaultDelay = 2000;
    }

    public void Dispose()
    {
        // Clean up (unhide all elements)
        ForceShowAllElements();

        PluginInterface.LanguageChanged -= LanguageChanged;
        Framework.Update -= OnFrameworkUpdate;
        CommandManager.RemoveHandler(CommandName);
        ChatGui.ChatMessage -= OnChatMessage;
        Config.OnSave -= OnConfigChanged;

        ConfigWindow.Dispose();
        WindowSystem.RemoveWindow(ConfigWindow);
    }


    private void LanguageChanged(string langCode)
    {
        Language.Culture = new CultureInfo(langCode);
    }

    private void LoadConfig(out Configuration configuration)
    {
        var existingConfig = PluginInterface.GetPluginConfig();
        configuration = (existingConfig is { Version: 6 })
            ? (Configuration)existingConfig
            : new Configuration();
        configuration.Initialize();
    }

    private void DrawUi() => WindowSystem.Draw();

    private void DrawConfigUi() => ConfigWindow.Toggle();


    private void FaderCommandHandler(string s, string arguments)
    {
        switch (arguments.Trim())
        {
            case "t" or "toggle":
                Enabled = !Enabled;
                ChatGui.Print(Enabled ? Language.ChatPluginEnabled : Language.ChatPluginDisabled);
                break;
            case "on":
                Enabled = true;
                ChatGui.Print(Language.ChatPluginEnabled);
                break;
            case "off":
                Enabled = false;
                ChatGui.Print(Language.ChatPluginDisabled);
                break;
            case "":
                ConfigWindow.Toggle();
                break;
        }
    }

    private void OnConfigChanged()
    {
        ConfigChanged = true;
    }

    private void OnChatMessage(XivChatType type, int _, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        // Don't trigger chat for non-standard chat channels.
        if (!Constants.ActiveChatTypes.Contains(type)
            && (!Config.ImportantActivity || !Constants.ImportantChatTypes.Contains(type))
            && (!Config.EmoteActivity || !Constants.EmoteChatTypes.Contains(type)))
            return;

        LastChatActivity = Environment.TickCount64;
    }

    private bool IsChatActive() => (Environment.TickCount64 - LastChatActivity) < Config.ChatActivityTimeout;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!IsSafeToWork())
            return;

        StateChanged = false;
        UpdateInputStates();
        UpdateMouseHoverState();

        if (StateChanged || ConfigChanged || !DoAlphasMatch() || AnyDelayExpired())
        {
            UpdateAddonOpacity();
            ConfigChanged = false;
        }
    }

    #region Input & State Management

    private void UpdateInputStates()
    {
        UpdateState(State.UserFocus, KeyState[Config.OverrideKey] || (Config.FocusOnHotbarsUnlock && !Addon.AreHotbarsLocked()));
        UpdateState(State.AltKeyFocus, KeyState[(int)Constants.OverrideKeys.Alt]);
        UpdateState(State.CtrlKeyFocus, KeyState[(int)Constants.OverrideKeys.Ctrl]);
        UpdateState(State.ShiftKeyFocus, KeyState[(int)Constants.OverrideKeys.Shift]);
        UpdateState(State.ChatFocus, Addon.IsChatFocused());
        UpdateState(State.ChatActivity, IsChatActive());
        UpdateState(State.IsMoving, Addon.IsMoving());
        UpdateState(State.Combat, Condition[ConditionFlag.InCombat]);
        UpdateState(State.WeaponUnsheathed, Addon.IsWeaponUnsheathed());
        UpdateState(State.InSanctuary, Addon.InSanctuary());
        UpdateState(State.InFate, Addon.InFate());

        UpdateState(State.LeftTrigger, Addon.IsControllerInputHeld(Dalamud.Game.ClientState.GamePad.GamepadButtons.L2));
        UpdateState(State.RightTrigger, Addon.IsControllerInputHeld(Dalamud.Game.ClientState.GamePad.GamepadButtons.R2));
        UpdateState(State.LeftBumper, Addon.IsControllerInputHeld(Dalamud.Game.ClientState.GamePad.GamepadButtons.L1));
        UpdateState(State.RightBumper, Addon.IsControllerInputHeld(Dalamud.Game.ClientState.GamePad.GamepadButtons.R1));

        var target = TargetManager.Target;
        UpdateState(State.EnemyTarget, target?.ObjectKind == ObjectKind.BattleNpc);
        UpdateState(State.PlayerTarget, target?.ObjectKind == ObjectKind.Player);
        UpdateState(State.NPCTarget, target?.ObjectKind == ObjectKind.EventNpc);
        UpdateState(State.Crafting, Condition[ConditionFlag.Crafting]);
        UpdateState(State.Gathering, Condition[ConditionFlag.Gathering]);
        UpdateState(State.Mounted, Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2]);

        var inIslandSanctuary = (TerritorySheet.TryGetRow(ClientState.TerritoryType, out var territory) && territory.TerritoryIntendedUse.RowId == 49);
        UpdateState(State.IslandSanctuary, inIslandSanctuary);

        var boundByDuty = Condition[ConditionFlag.BoundByDuty]
                          || Condition[ConditionFlag.BoundByDuty56]
                          || Condition[ConditionFlag.BoundByDuty95];
        UpdateState(State.Duty, !inIslandSanctuary && boundByDuty);

        var occupied = Condition[ConditionFlag.Occupied]
          || Condition[ConditionFlag.Occupied30]
          || Condition[ConditionFlag.Occupied33]
          || Condition[ConditionFlag.Occupied38]
          || Condition[ConditionFlag.Occupied39]
          || Condition[ConditionFlag.OccupiedInCutSceneEvent]
          || Condition[ConditionFlag.OccupiedInEvent]
          || Condition[ConditionFlag.OccupiedSummoningBell]
          || Condition[ConditionFlag.OccupiedInQuestEvent];

        UpdateState(State.Occupied, occupied);
    }

    /// <summary>
    /// Collects all addon hover states
    /// </summary>
    private void UpdateHoverStates()
    {
        var mousePos = ImGui.GetMousePos();
        AddonHoverStates.Clear();

        foreach (var addonName in AddonNameToElement.Keys)
        {
            // Compute the hover state once per addon.
            AddonHoverStates[addonName] = IsAddonHovered(addonName, mousePos);
        }
    }


    private void UpdateMouseHoverState()
    {
        // Update the hover states for all addons.
        UpdateHoverStates();

        var currentHovered = new HashSet<string>(
            AddonHoverStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key)
        );

        if (!currentHovered.SequenceEqual(PreviousHoveredAddons))
            StateChanged = true;

        PreviousHoveredAddons = currentHovered;

        var hoverDetected = currentHovered.Count != 0;
        UpdateState(State.Hover, hoverDetected);
    }



    private void UpdateState(State state, bool value)
    {
        if (StateMap[state] != value)
        {
            StateMap[state] = value;
            StateChanged = true;
        }
    }

    #endregion

    #region Opacity & Visibility Management

    private void UpdateAddonOpacity()
    {
        if (!IsSafeToWork())
            return;

        var forceShow = !Enabled || Addon.IsHudManagerOpen();

        if (forceShow)
        {
            foreach (var addonName in AddonNameToElement.Keys)
            {
                Addon.SetAddonVisibility(addonName, true);
                FinishingHover[addonName] = false;
            }
            return;
        }

        // If delay is disabled, clear any stored delay state.
        if (!Config.DefaultDelayEnabled)
        {
            DelayTimers.Clear();
            LastNonDefaultEntry.Clear();
        }

        foreach (var addonName in AddonNameToElement.Keys)
        {
            var element = AddonNameToElement[addonName];
            var elementConfig = Config.GetElementConfig(element);
            var isHovered = AddonHoverStates.TryGetValue(addonName, out var hovered) && hovered;

            var candidate = GetCandidateConfig(addonName, elementConfig, isHovered);
            var effectiveSetting = GetEffectiveSetting(candidate);

            var currentAlpha = CurrentAlphas.TryGetValue(addonName, out var alpha) ? alpha : Config.DefaultAlpha;
            var targetAlpha = CalculateTargetAlpha(candidate, effectiveSetting, isHovered, currentAlpha);

            TargetAlphas[addonName] = targetAlpha;

            var isHoverState = (candidate.state == State.Hover);

            if (isHovered || (FinishingHover.TryGetValue(addonName, out var finishing) && finishing))
            {
                if (isHoverState)
                    FinishingHover[addonName] = true;

                if (currentAlpha < candidate.Opacity - 0.001f)
                {
                    targetAlpha = candidate.Opacity;
                    TargetAlphas[addonName] = targetAlpha;
                }
                else if (!isHovered)
                {
                    FinishingHover[addonName] = false;
                }
            }
            else
            {
                FinishingHover[addonName] = false;
            }

            var transitionSpeed = (targetAlpha > currentAlpha)
                ? Config.EnterTransitionSpeed
                : Config.ExitTransitionSpeed;

            currentAlpha = MoveTowards(currentAlpha, targetAlpha, transitionSpeed * (float)Framework.UpdateDelta.TotalSeconds);
            CurrentAlphas[addonName] = currentAlpha;
            Addon.SetAddonOpacity(addonName, currentAlpha);

            var defaultDisabled = (candidate.state == State.Default && candidate.setting == Setting.Hide);
            var hidden = defaultDisabled && currentAlpha < 0.05f;
            Addon.SetAddonVisibility(addonName, !hidden);
        }
    }


    private ConfigEntry GetCandidateConfig(string addonName, List<ConfigEntry> elementConfig, bool isHovered)
    {
        // Prefer Hover state when applicable.
        var candidate = isHovered
            ? elementConfig.FirstOrDefault(e => e.state == State.Hover)
            : null;

        // Fallback: choose an active non-hover state or default.
        candidate ??= elementConfig.FirstOrDefault(e => StateMap[e.state] && e.state != State.Hover)
                        ?? elementConfig.FirstOrDefault(e => e.state == State.Default);

        var now = Environment.TickCount64;
        if (candidate != null && candidate.state != State.Default)
        {
            // Record the non-default state with a timestamp.
            DelayTimers[addonName] = now;
            LastNonDefaultEntry[addonName] = candidate;

            // Force non-default states to Show.
            if (candidate.setting == Setting.Hide)
            {
                candidate.setting = Setting.Show;
            }
        }
        else if (candidate != null && candidate.state == State.Default && Config.DefaultDelayEnabled)
        {
            // Check if there's a recent non-default state that should be used.
            if (DelayTimers.TryGetValue(addonName, out var start) &&
                (now - start) < Config.DefaultDelay)
            {
                if (LastNonDefaultEntry.TryGetValue(addonName, out var nonDefault))
                {
                    candidate = nonDefault;
                }
            }
            else
            {
                // Delay expired; clear stored values.
                DelayTimers.Remove(addonName);
                LastNonDefaultEntry.Remove(addonName);
            }
        }

        return candidate!;
    }


    private Setting GetEffectiveSetting(ConfigEntry candidate)
    {
        if (!Enabled || Addon.IsHudManagerOpen())
            return Setting.Show;
        return candidate.setting;
    }

    private static float CalculateTargetAlpha(ConfigEntry candidate, Setting effectiveSetting, bool isHovered, float currentAlpha)
    {
        if (candidate.state == State.Hover)
        {
            return isHovered ? candidate.Opacity : currentAlpha;
        }
        return (effectiveSetting == Setting.Show) ? candidate.Opacity : 0.0f;
    }

    /// <summary>
    /// Smoothly moves current value toward target value.
    /// </summary>
    private static float MoveTowards(float current, float target, float maxDelta)
    {
        // TODO: add easing functions perhaps?
        if (Math.Abs(target - current) <= maxDelta)
            return target;
        return current + Math.Sign(target - current) * maxDelta;
    }

    /// <summary>
    /// Checks if the addon identified by addonName is currently hovered.
    /// </summary>
    private unsafe bool IsAddonHovered(string addonName, Vector2 mousePos)
    {
        var addonPointer = GameGui.GetAddonByName(addonName);
        if (addonPointer == nint.Zero)
            return false;

        var addon = (AtkUnitBase*)addonPointer;
        float posX = addon->GetX();
        float posY = addon->GetY();
        var width = addon->GetScaledWidth(true);
        var height = addon->GetScaledHeight(true);

        return mousePos.X >= posX && mousePos.X <= posX + width &&
               mousePos.Y >= posY && mousePos.Y <= posY + height;
    }

    /// <summary>
    /// Forces all elements to be visible and fully opaque.
    /// </summary>
    private void ForceShowAllElements()
    {
        foreach (var addonName in AddonNameToElement.Keys)
        {
            Addon.SetAddonOpacity(addonName, 1.0f);
            Addon.SetAddonVisibility(addonName, true);
        }
    }


    #endregion

    #region Helper Methods
    private bool DoAlphasMatch()
    {
        // Check if both dictionaries have the same number of entries.
        if (TargetAlphas.Count != CurrentAlphas.Count)
            return false;

        foreach (var kvp in TargetAlphas)
        {
            if (!CurrentAlphas.TryGetValue(kvp.Key, out var currentAlpha) ||
                Math.Abs(currentAlpha - kvp.Value) > 0.001f)
            {
                return false;
            }
        }
        return true;
    }

    private bool AnyDelayExpired()
    {
        var now = Environment.TickCount64;
        return DelayTimers.Values.Any(timer => (now - timer) >= Config.DefaultDelay);
    }

    /// <summary>
    /// Checks if it is safe for the plugin to perform work.
    /// </summary>
    private bool IsSafeToWork() => !Condition[ConditionFlag.BetweenAreas] && ClientState.IsLoggedIn;

    #endregion
}
