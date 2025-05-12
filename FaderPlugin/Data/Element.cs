using faderPlugin.Resources;
using System;
using System.Collections.Generic;

namespace FaderPlugin.Data;

// Do not change the values of existing states, it will break configs
public enum Element
{
    Unknown = 0,

    Hotbar1 = 1,
    Hotbar2 = 2,
    Hotbar3 = 3,
    Hotbar4 = 4,
    Hotbar5 = 5,
    Hotbar6 = 6,
    Hotbar7 = 7,
    Hotbar8 = 8,
    Hotbar9 = 9,
    Hotbar10 = 10,
    CrossHotbar = 11,
    PetHotbar = 12,
    ContextActionHotbar = 13,
    Job = 14,
    CastBar = 15,
    ExperienceBar = 16,
    InventoryGrid = 17,
    Currency = 18,
    ScenarioGuide = 19,
    QuestLog = 20,
    DutyList = 21,
    ServerInfo = 22,
    IslekeepIndex = 23,
    MainMenu = 24,
    Chat = 25,
    Minimap = 26,
    Nameplates = 27,
    TargetInfo = 28,
    PartyList = 29,
    LimitBreak = 30,
    Parameters = 31,
    Status = 32,
    StatusEnhancements = 33,
    StatusEnfeeblements = 34,
    StatusOther = 35,
    StatusConditional = 36,
    CosmicExotablet = 37,

}

public static class ElementUtil
{
    public static string GetElementName(Element element)
    {
        return element switch
        {
            Element.Hotbar1 => Language.ElementHotbar1,
            Element.Hotbar2 => Language.ElementHotbar2,
            Element.Hotbar3 => Language.ElementHotbar3,
            Element.Hotbar4 => Language.ElementHotbar4,
            Element.Hotbar5 => Language.ElementHotbar5,
            Element.Hotbar6 => Language.ElementHotbar6,
            Element.Hotbar7 => Language.ElementHotbar7,
            Element.Hotbar8 => Language.ElementHotbar8,
            Element.Hotbar9 => Language.ElementHotbar9,
            Element.Hotbar10 => Language.ElementHotbar10,
            Element.CrossHotbar => Language.ElementCrossHotbar,
            Element.PetHotbar => Language.ElementPetHotbar,
            Element.ContextActionHotbar => Language.ElementContextActionHotbar,
            Element.CastBar => Language.ElementCastbar,
            Element.ExperienceBar => Language.ElementExperienceBar,
            Element.InventoryGrid => Language.ElementInventoryGrid,
            Element.ScenarioGuide => Language.ElementScenarioGuide,
            Element.IslekeepIndex => Language.ElementIslekeepIndex,
            Element.CosmicExotablet => Language.ElementCosmicExotablet,
            Element.DutyList => Language.ElementDutyList,
            Element.ServerInfo => Language.ElementServerInformation,
            Element.MainMenu => Language.ElementMainMenu,
            Element.TargetInfo => Language.ElementTargetInfo,
            Element.PartyList => Language.ElementPartyList,
            Element.LimitBreak => Language.ElementLimitBreak,
            Element.StatusEnhancements => Language.ElementStatusEnhancements,
            Element.StatusEnfeeblements => Language.ElementStatusEnfeeblements,
            Element.StatusOther => Language.ElementStatusOther,
            Element.StatusConditional => Language.ElementStatusConditional,
            Element.Unknown => Language.ElementUnknown,
            Element.Job => Language.ElementJob,
            Element.Currency => Language.ElementCurrency,
            Element.QuestLog => Language.ElementQuestLog,
            Element.Chat => Language.ElementChat,
            Element.Minimap => Language.ElementMinimap,
            Element.Nameplates => Language.ElementNameplate,
            Element.Parameters => Language.ElementParameters,
            Element.Status => Language.ElementStatus,
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, null)
        };
    }

    public static string[] GetAddonName(Element element)
    {
        return element switch
        {
            Element.Hotbar1 => ["_ActionBar"],
            Element.Hotbar2 => ["_ActionBar01"],
            Element.Hotbar3 => ["_ActionBar02"],
            Element.Hotbar4 => ["_ActionBar03"],
            Element.Hotbar5 => ["_ActionBar04"],
            Element.Hotbar6 => ["_ActionBar05"],
            Element.Hotbar7 => ["_ActionBar06"],
            Element.Hotbar8 => ["_ActionBar07"],
            Element.Hotbar9 => ["_ActionBar08"],
            Element.Hotbar10 => ["_ActionBar09"],
            Element.CrossHotbar =>
            [
                "_ActionCross",
                "_ActionDoubleCrossL",
                "_ActionDoubleCrossR"
            ],
            Element.ContextActionHotbar => ["_ActionContents"],
            Element.PetHotbar => ["_ActionBarEx"],
            Element.Job =>
            [
                "JobHudPLD0",
                "JobHudWAR0",
                "JobHudDRK0", "JobHudDRK1",
                "JobHudGNB0",
                "JobHudWHM0",
                "JobHudACN0", "JobHudSCH0",
                "JobHudAST0",
                "JobHudGFF0", "JobHudGFF1",
                "JobHudMNK0", "JobHudMNK1",
                "JobHudDRG0",
                "JobHudNIN0", "JobHudNIN1v70",
                "JobHudSAM0", "JobHudSAM1",
                "JobHudRRP0", "JobHudRRP1",
                "JobHudBRD0",
                "JobHudMCH0",
                "JobHudDNC0", "JobHudDNC1",
                "JobHudBLM0", "JobHudBLM1",
                "JobHudSMN0", "JobHudSMN1",
                "JobHudRDM0",
                "JobHudRPM0", "JobHudRPM1",
                "JobHudRDB0", "JobHudRDB1",
            ],
            Element.PartyList => ["_PartyList"],
            Element.LimitBreak => ["_LimitBreak"],
            Element.Parameters => ["_ParameterWidget"],
            Element.Status => ["_Status"],
            Element.StatusEnhancements => ["_StatusCustom0"],
            Element.StatusEnfeeblements => ["_StatusCustom1"],
            Element.StatusOther => ["_StatusCustom2"],
            Element.StatusConditional => ["_StatusCustom3"],
            Element.CastBar => ["_CastBar"],
            Element.ExperienceBar => ["_Exp"],
            Element.ScenarioGuide => ["ScenarioTree"],
            Element.InventoryGrid => ["_BagWidget"],
            Element.DutyList => ["_ToDoList"],
            Element.ServerInfo => ["_DTR"],
            Element.IslekeepIndex => ["MJIHud"],
            Element.CosmicExotablet => ["WKSHud"],
            Element.MainMenu => ["_MainCommand"],
            Element.Chat =>
            [
                "ChatLog",
                "ChatLogPanel_0",
                "ChatLogPanel_1",
                "ChatLogPanel_2",
                "ChatLogPanel_3"
            ],
            Element.Minimap => ["_NaviMap"],
            Element.Currency => ["_Money"],
            Element.TargetInfo =>
            [
                "_TargetInfoMainTarget",
                "_TargetInfoBuffDebuff",
                "_TargetInfoCastBar",
                "_TargetInfo"
            ],
            Element.Unknown => [],
            _ => [],
        };
    }

    public static string TooltipForElement(this Element elementId)
    {
        return elementId switch
        {
            Element.Chat => Language.ElementTooltipChat,
            Element.CrossHotbar => Language.ElementTooltipCrosshotbar,
            Element.ContextActionHotbar => Language.ElementTooltipActionHotbar,
            Element.PetHotbar => Language.ElementTooltipPetHotbar,
            Element.Job => Language.ElementTooltipJob,
            Element.Status => Language.ElementTooltipStatus,
            Element.StatusEnfeeblements => Language.ElementTooltipStatusEnfeeblements,
            Element.StatusEnhancements => Language.ElementTooltipStatusEnhancements,
            Element.StatusOther => Language.ElementTooltipStatusOther,
            Element.StatusConditional => Language.ElementTooltipStatusConditional,
            _ => string.Empty,
        };
    }

    public static bool ShouldIgnoreElement(this Element elementId)
    {
        return elementId switch
        {
            Element.QuestLog => true,
            Element.Nameplates => true,
            Element.Unknown => true,
            _ => false,
        };
    }

    public static readonly List<Element> OrderedElements =
    [
        Element.Unknown,
        Element.Hotbar1,
        Element.Hotbar2,
        Element.Hotbar3,
        Element.Hotbar4,
        Element.Hotbar5,
        Element.Hotbar6,
        Element.Hotbar7,
        Element.Hotbar8,
        Element.Hotbar9,
        Element.Hotbar10,
        Element.CrossHotbar,
        Element.PetHotbar,
        Element.ContextActionHotbar,
        Element.Job,
        Element.CastBar,
        Element.ExperienceBar,
        Element.InventoryGrid,
        Element.Currency,
        Element.ScenarioGuide,
        Element.QuestLog,
        Element.DutyList,
        Element.ServerInfo,
        Element.IslekeepIndex,
        Element.CosmicExotablet,
        Element.MainMenu,
        Element.Chat,
        Element.Minimap,
        Element.Nameplates,
        Element.TargetInfo,
        Element.PartyList,
        Element.LimitBreak,
        Element.Parameters,
        Element.Status,
        Element.StatusEnhancements,
        Element.StatusEnfeeblements,
        Element.StatusOther,
        Element.StatusConditional,
    ];
}