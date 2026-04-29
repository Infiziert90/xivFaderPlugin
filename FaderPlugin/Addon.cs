using Dalamud.Game.ClientState.GamePad;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Misc;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace FaderPlugin;

public static unsafe class Addon
{

    private static readonly AtkStage* Stage = AtkStage.Instance();

    private static readonly Dictionary<string, (short X, short Y)> StoredPositions = [];

    #region Visibility and Position

    /// <summary>
    /// Returns the HUD-layout’s saved opacity [0..1] for this addon.
    /// </summary>
    public static float GetSavedOpacity(string addonName)
    {
        var config = AddonConfig.Instance();
        if (config == null || config->ActiveDataSet == null)
            return 1.0f;

        var data = config->ActiveDataSet;
        var addons = data->HudLayoutConfigEntries;     // 440
        var layouts = data->HudLayoutNames.Length;     // 4
        var addonsPerLayout = addons.Length / layouts; // 440/4 = 110
        var currentLayout = data->CurrentHudLayout;    // 0..3
        var start = currentLayout * addonsPerLayout;
        var end = start + addonsPerLayout;

        // Hash our addonNames & find corresponding addon in config
        var keyString = addonName + "_a";
        var rawHash = Crc32.Get(keyString);
        var addonNameHash = ~rawHash;
        for (var i = start; i < end; i++)
        {
            if (!addons[i].HasValue) continue;
            if (addons[i].AddonNameHash != addonNameHash) continue;

            // all special HudElements that don't have an opacity slider have an alpha value of 0. Elements that do have a slider only go down to ~0,1
            if (addons[i].Alpha == 0)
                return 1.0f;

            return addons[i].Alpha / 255f;
        }
        // fallback if not found (e.g. Chat, since you can't natively adjust its opacity)
        return 1.0f;
    }

    public static void SetAddonVisibility(string name, bool isVisible)
    {
        var addonPointer = Plugin.GameGui.GetAddonByName(name);
        if (addonPointer.IsNull)
            return;

        if (isVisible)
        {
            // Restore the element position if previously hidden.
            if (StoredPositions.TryGetValue(name, out var pos) && (addonPointer.X == -9999 || addonPointer.Y == -9999))
                ((AtkUnitBase*)addonPointer.Address)->SetPosition(pos.X, pos.Y);
        }
        else
        {
            // Save position, then move off-screen.
            if (addonPointer.X != -9999 && addonPointer.Y != -9999)
                StoredPositions[name] = (addonPointer.X, addonPointer.Y);

            ((AtkUnitBase*)addonPointer.Address)->SetPosition(-9999, -9999);
        }
    }

    /// <summary>
    /// Sets the alpha (transparency) of an addon.
    /// Value range is 0.0f (fully transparent) to 1.0f (fully opaque).
    /// </summary>
    /// <param name="addonName">The name of the addon.</param>
    /// <param name="alpha">Alpha in the range [0..1].</param>
    public static void SetAddonOpacity(string addonName, float alpha)
    {
        var addonPointer = Plugin.GameGui.GetAddonByName(addonName);
        if (addonPointer == nint.Zero)
            return;

        var addon = (AtkUnitBase*)addonPointer.Address;
        if (addon->UldManager.NodeListCount <= 0)
            return;

        var node = addon->UldManager.NodeList[0];
        if (node == null)
            return;

        // Preserve RGB, only adjust alpha.
        var currentColor = node->Color;
        var newAlpha = (byte)(alpha * 255);

        ByteColor newColor = default;
        newColor.R = currentColor.R;
        newColor.G = currentColor.G;
        newColor.B = currentColor.B;
        newColor.A = newAlpha;

        node->Color = newColor;
    }

    public static AddonPosition GetAddonPosition(string name)
    {
        var addonPointer = Plugin.GameGui.GetAddonByName(name);
        if (addonPointer == nint.Zero)
            return AddonPosition.Empty;

        var addon = (AtkUnitBase*)addonPointer.Address;
        var width = (short)addon->GetScaledWidth(true);
        var height = (short)addon->GetScaledHeight(true);

        return new AddonPosition(true, addon->X, addon->Y, width, height);
    }

    #endregion

    #region Addon Open/Close State

    private static bool IsAddonOpen(string name)
        => Plugin.GameGui.GetAddonByName(name) != nint.Zero;

    #endregion

    #region Focus / Chat / HUD

    public static bool IsHudManagerOpen()
        => IsAddonOpen("HudLayout");

    public static bool IsChatFocused()
    {
        return IsAddonFocused("ChatLog")
            || IsAddonFocused("ChatLogPanel_0")
            || IsAddonFocused("ChatLogPanel_1")
            || IsAddonFocused("ChatLogPanel_2")
            || IsAddonFocused("ChatLogPanel_3");
    }


    private static bool IsAddonFocused(string name)
    {
        foreach (var addon in Stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.Entries)
        {
            if (addon.Value == null || addon.Value->Name.IsEmpty)
                continue;

            if (name.Equals(addon.Value->NameString))
                return true;
        }

        return false;
    }

    #endregion

    #region Mouse / Movement Checks

    public static bool IsMoving()
        => AgentMap.Instance()->IsPlayerMoving;


    public static bool AreHotbarsLocked()
    {
        var hotbar = Plugin.GameGui.GetAddonByName("_ActionBar");
        var crossbar = Plugin.GameGui.GetAddonByName("_ActionCross");
        if (hotbar == nint.Zero || crossbar == nint.Zero)
            return true;

        var hotbarAddon = (AddonActionBar*)hotbar.Address;
        var crossbarAddon = (AddonActionCross*)hotbar.Address;

        try
        {
            // Check whether Mouse Mode or Gamepad Mode is enabled.
            var mouseModeEnabled = hotbarAddon->ShowHideFlags == 0;
            return mouseModeEnabled ? hotbarAddon->IsLocked : crossbarAddon->IsLocked;
        }
        catch (AccessViolationException)
        {
            return true;
        }
    }

    #endregion

    #region Combat / World State Checks

    public static bool IsWeaponUnsheathed()
        => UIState.Instance()->WeaponState.IsUnsheathed;

    public static bool InSanctuary()
        => TerritoryInfo.Instance()->InSanctuary;

    public static bool InFate()
        => FateManager.Instance()->CurrentFate != null;

    #endregion

    #region Controller Input Check

    public static bool IsControllerInputHeld(GamepadButtons button)
        => Plugin.GamepadState.Raw(button) != 0;

    #endregion

    #region Helper Record

    public record AddonPosition(bool IsPresent, short X, short Y, short W, short H)
    {
        public Vector2 Start => new(X, Y);
        public Vector2 End => new(X + W, Y + H);

        public static AddonPosition Empty => new(false, 0, 0, 0, 0);
    }

    #endregion
}
