using faderPlugin.Resources;
using System;
using System.Collections.Generic;

namespace FaderPlugin.Data;

// Do not change the values of existing states, it will break configs
public enum State
{
    None = 0,
    Default = 1,
    Duty = 2,
    EnemyTarget = 3,
    PlayerTarget = 4,
    NPCTarget = 5,
    Crafting = 6,
    Gathering = 7,
    Mounted = 8,
    Combat = 9,
    WeaponUnsheathed = 10,
    IslandSanctuary = 11,
    ChatFocus = 12,
    UserFocus = 13,
    ChatActivity = 14,
    AltKeyFocus = 15,
    CtrlKeyFocus = 16,
    ShiftKeyFocus = 17,
    InSanctuary = 18,
    InFate = 19,
    IsMoving = 20,
    Hover = 21,
    Occupied = 22,
    LeftTrigger = 23,
    RightTrigger = 24,
    LeftBumper = 25,
    RightBumper = 26,
}


public static class StateUtil
{
    public static string GetStateName(State state)
    {
        return state switch
        {
            State.EnemyTarget => Language.StateEnemyTarget,
            State.PlayerTarget => Language.StatePlayerTarget,
            State.NPCTarget => Language.StateNPCTarget,
            State.WeaponUnsheathed => Language.StateWeaponUnsheathed,
            State.InSanctuary => Language.StateInSanctuary,
            State.InFate => Language.StateInFateArea,
            State.IsMoving => Language.StateIsMoving,
            State.IslandSanctuary => Language.StateIslandSanctuary,
            State.ChatActivity => Language.StateChatActivity,
            State.ChatFocus => Language.StateChatFocus,
            State.UserFocus => Language.StateUserFocus,
            State.AltKeyFocus => Language.StateAltKey,
            State.CtrlKeyFocus => Language.StateCtrlKey,
            State.ShiftKeyFocus => Language.StateShiftKey,
            State.None => Language.StateNone,
            State.Default => Language.StateDefault,
            State.Duty => Language.StateDuty,
            State.Crafting => Language.StateCrafting,
            State.Gathering => Language.StateGathering,
            State.Mounted => Language.StateMounted,
            State.Combat => Language.StateCombat,
            State.Hover => Language.StateHover,
            State.Occupied => Language.StateOccupied,
            State.LeftTrigger => Language.StateLeftTrigger,
            State.RightTrigger => Language.StateRightTrigger,
            State.LeftBumper => Language.StateLeftBumper,
            State.RightBumper => Language.StateRightBumper,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    public static readonly List<State> OrderedStates =
    [
        State.None,
        State.Default,
        State.Duty,
        State.EnemyTarget,
        State.PlayerTarget,
        State.NPCTarget,
        State.Crafting,
        State.Gathering,
        State.IsMoving,
        State.Mounted,
        State.Combat,
        State.WeaponUnsheathed,
        State.InSanctuary,
        State.InFate,
        State.IslandSanctuary,
        State.ChatActivity,
        State.ChatFocus,
        State.UserFocus,
        State.AltKeyFocus,
        State.CtrlKeyFocus,
        State.ShiftKeyFocus,
        State.Hover,
        State.Occupied,
        State.LeftTrigger,
        State.RightTrigger,
        State.LeftBumper,
        State.RightBumper,
    ];
}
