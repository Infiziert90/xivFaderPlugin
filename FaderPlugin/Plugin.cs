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
using FaderPlugin.Animation;
using FaderPlugin.Data;
using FaderPlugin.Windows.Config;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
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
    private bool HudManagerPrevOpened = false;

    // Opacity Management
    private readonly Dictionary<string, float> CurrentAlphas = [];
    private readonly Dictionary<string, float> TargetAlphas = [];
    // smallest possible alpha change
    private const float AlphaTolerance = 1f / 255f;

    // Tween management
    private readonly Dictionary<string, Tween> Tweens = [];

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
        // Clean up (unhide all elements & set Opacity to HUDLayout values)
        RestoreGameOpacity();
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
                if (!Enabled) RestoreGameOpacity();
                ChatGui.Print(Enabled ? Language.ChatPluginEnabled : Language.ChatPluginDisabled);
                break;
            case "on":
                Enabled = true;
                ChatGui.Print(Language.ChatPluginEnabled);
                break;
            case "off":
                Enabled = false;
                RestoreGameOpacity();
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

        var hudOpen = Addon.IsHudManagerOpen();
        var forceShow = !Enabled || hudOpen;

        if (hudOpen && !HudManagerPrevOpened)
            RestoreGameOpacity();
        HudManagerPrevOpened = hudOpen;

        if (forceShow) return;

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
        UpdateState(State.GatheringNodeTarget, target?.ObjectKind == ObjectKind.GatheringPoint);
        UpdateState(State.Crafting, Condition[ConditionFlag.Crafting]);
        UpdateState(State.Gathering, Condition[ConditionFlag.Gathering]);
        UpdateState(State.Mounted, Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.RidingPillion]);

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
    /// For each hover group defined in configuration, if any addon in that group is hovered,
    /// mark all addons in the group as hovered.
    /// </summary>
    private void ApplyHoverGroups()
    {
        var originalHoverStates = new Dictionary<string, bool>(AddonHoverStates);
        var finalHoverStates = new Dictionary<string, bool>(originalHoverStates);

        foreach (var group in Config.HoverGroups)
        {
            var groupAddonNames = AddonNameToElement
                .Where(kvp => group.Elements.Contains(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            if (groupAddonNames.Count == 0)
                continue;

            var groupActivated = groupAddonNames.Any(addonName =>
                originalHoverStates.TryGetValue(addonName, out var hovered) && hovered);

            if (groupActivated)
            {
                // Only update final states based on the activation of this group.
                // This is to prevent cascading hover states, where multiple groups have overlapping addons.
                foreach (var addonName in groupAddonNames)
                {
                    finalHoverStates[addonName] = true;
                }
            }
        }

        // Update the global hover states with our computed final states.
        foreach (var kvp in finalHoverStates)
        {
            AddonHoverStates[kvp.Key] = kvp.Value;
        }
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
        ApplyHoverGroups();
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
        // If delay is disabled, clear any stored delay state.
        if (!Config.DefaultDelayEnabled)
        {
            DelayTimers.Clear();
            LastNonDefaultEntry.Clear();
        }

        var now = Environment.TickCount64;
        foreach (var addonName in AddonNameToElement.Keys)
        {
            var element = AddonNameToElement[addonName];
            var elementConfig = Config.GetElementConfig(element);
            var currentAddonHovered = AddonHoverStates.TryGetValue(addonName, out var hovered) && hovered;

            var candidate = GetCandidateConfig(addonName, elementConfig, currentAddonHovered);
            var currentAlpha = CurrentAlphas.TryGetValue(addonName, out var alpha) ? alpha : Config.DefaultAlpha;
            var targetAlpha = GetTargetAlpha(addonName, candidate, currentAddonHovered, currentAlpha);

            TargetAlphas[addonName] = targetAlpha;

            // animation
            var transitionSpeed = GetTransitionSpeed(element, currentAlpha, targetAlpha);
            var duration = transitionSpeed > 0f ? (long)((1f / transitionSpeed) * 1000f) : 0L;

            if (duration <= 0)
            {
                CurrentAlphas[addonName] = targetAlpha;
                Addon.SetAddonOpacity(addonName, targetAlpha);
                Tweens.Remove(addonName);
            }
            else
            {
                if (!Tweens.TryGetValue(addonName, out var tween) || Math.Abs(tween.EndValue - targetAlpha) > AlphaTolerance)
                {
                    tween = new Tween(currentAlpha, targetAlpha, now, duration, Easing.Linear);
                    Tweens[addonName] = tween;
                }

                var newAlpha = tween.Value(now);
                CurrentAlphas[addonName] = newAlpha;
                Addon.SetAddonOpacity(addonName, newAlpha);

                if (tween.IsComplete(now))
                    Tweens.Remove(addonName);
            }

            // visibility
            var isElementDisabled = Config.DisabledElements.TryGetValue(element, out var disabled) && disabled;
            var shouldHide = isElementDisabled && currentAlpha < 0.05f;
            Addon.SetAddonVisibility(addonName, !shouldHide);
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

    /// <summary>
    /// Returns the native Opacity of an addon when Relative Opacity is enabled. Returns 1f when disabled.
    /// </summary>
    private float GetAlphaModifier(string addonName)
    {
        if (!Config.RelativeOpacity)
            return 1f;


        return Addon.GetSavedOpacity(addonName);
    }

    private float GetTargetAlpha(string addonName, ConfigEntry candidate, bool currentAddonHovered, float currentAlpha)
    {
        var anyAddonHovered = candidate.state == State.Hover;
        // if any addon is hovered and it isn't the current addon then keep the opacity the same for the current addon otherwise go to target
        var baseAlpha = anyAddonHovered && !currentAddonHovered ? currentAlpha : candidate.Opacity;

        var alphaModifier = GetAlphaModifier(addonName);
        var targetAlpha = baseAlpha * alphaModifier;

        if (anyAddonHovered)
        {
            var fullAlpha = candidate.Opacity * alphaModifier;
            if (currentAlpha < fullAlpha - AlphaTolerance)
            {
                // override targetAlpha so that current addon doesn't get locked at currentAlpha when another addon is hovered
                return fullAlpha;
            }
        }
        return targetAlpha;
    }

    private float GetTransitionSpeed(Element element, float currentAlpha, float targetAlpha)
    {
        if (Config.FadeOverrides.TryGetValue(element, out var fadeOverride) && fadeOverride.UseCustomFadeTimes)
        {
            return targetAlpha > currentAlpha
                ? fadeOverride.EnterTransitionSpeedOverride
                : fadeOverride.ExitTransitionSpeedOverride;
        }

        return targetAlpha > currentAlpha
            ? Config.EnterTransitionSpeed
            : Config.ExitTransitionSpeed;
    }

    /// <summary>
    /// Checks if the addon identified by addonName is currently hovered.
    /// </summary>
    private unsafe bool IsAddonHovered(string addonName, Vector2 mousePos)
    {
        var addonPointer = GameGui.GetAddonByName(addonName);
        if (addonPointer == nint.Zero)
            return false;

        var addon = (AtkUnitBase*)addonPointer.Address;
        float posX = addon->GetX();
        float posY = addon->GetY();
        var width = addon->GetScaledWidth(true);
        var height = addon->GetScaledHeight(true);

        return mousePos.X >= posX && mousePos.X <= posX + width &&
               mousePos.Y >= posY && mousePos.Y <= posY + height;
    }

    /// <summary>
    /// Forces all elements to be visible and return to using In-Game opacity values.
    /// </summary>
    private void RestoreGameOpacity()
    {
        foreach (var kvp in AddonNameToElement)
        {
            var addonName = kvp.Key;
            var savedOpacity = Addon.GetSavedOpacity(addonName);
            CurrentAlphas[addonName] = savedOpacity;
            TargetAlphas[addonName] = savedOpacity;
            Addon.SetAddonOpacity(addonName, savedOpacity);
            Addon.SetAddonVisibility(addonName, true);
            Tweens.Remove(addonName);
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
                Math.Abs(currentAlpha - kvp.Value) > AlphaTolerance)
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
