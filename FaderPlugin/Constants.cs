﻿using Dalamud.Game.Text;
using System.Collections.Generic;

namespace FaderPlugin;

public static class Constants
{
    public static readonly List<XivChatType> ActiveChatTypes =
    [
        XivChatType.Say,
        XivChatType.Party,
        XivChatType.Alliance,
        XivChatType.Yell,
        XivChatType.Shout,
        XivChatType.FreeCompany,
        XivChatType.TellIncoming,
        XivChatType.Ls1,
        XivChatType.Ls2,
        XivChatType.Ls3,
        XivChatType.Ls4,
        XivChatType.Ls5,
        XivChatType.Ls6,
        XivChatType.Ls7,
        XivChatType.Ls8,
        XivChatType.CrossLinkShell1,
        XivChatType.CrossLinkShell2,
        XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4,
        XivChatType.CrossLinkShell5,
        XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7,
        XivChatType.CrossLinkShell8
    ];

    public static readonly List<XivChatType> EmoteChatTypes =
    [
        XivChatType.CustomEmote,
        XivChatType.StandardEmote
    ];

    public static readonly List<XivChatType> ImportantChatTypes =
    [
        XivChatType.Debug,
        XivChatType.Echo,
        XivChatType.SystemError,
        XivChatType.SystemMessage,
        XivChatType.ErrorMessage

    ];

    public enum OverrideKeys
    {
        Alt = 0x12,
        Ctrl = 0x11,
        Shift = 0x10,
    }
}