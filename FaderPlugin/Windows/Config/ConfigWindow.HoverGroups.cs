using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using faderPlugin.Resources;
using FaderPlugin.Data;
using Dalamud.Bindings.ImGui;
using System.Linq;
using System.Numerics;

namespace FaderPlugin.Windows.Config;

public partial class ConfigWindow
{
    private int SelectedHoverGroupIndex = -1;

    private void HoverGroups()
    {
        using var tabItem = ImRaii.TabItem(Language.TabHoverGroups);
        if (!tabItem.Success)
            return;

        var style = ImGui.GetStyle();
        var buttonWidth = ImGui.CalcTextSize("Context Action Hotbar   ?").X
                          + style.FramePadding.X * 2
                          + style.ScrollbarSize;
        var childSize = buttonWidth + style.WindowPadding.X * 2;

        // Left Pane: List of groups.
        using (var leftChild = ImRaii.Child("HoverGroupsList", new Vector2(childSize, 0), true))
        {
            if (leftChild.Success)
            {
                for (var i = 0; i < Configuration.HoverGroups.Count; i++)
                {
                    var group = Configuration.HoverGroups[i];
                    var buttonText = group.GroupName;

                    var label = $"{buttonText}##HoverGroup{i}";

                    using var pushedStyle = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));

                    var desiredButtonColor = ImGui.GetColorU32(ImGuiCol.Button);
                    if (SelectedHoverGroupIndex == i)
                        desiredButtonColor = ImGui.GetColorU32(ImGuiColors.HealerGreen);

                    var hasScrollbar = ImGui.GetScrollMaxY() > 0.0f;
                    using var pushedColor = ImRaii.PushColor(ImGuiCol.Button, desiredButtonColor);

                    if (ImGui.Button(label, new Vector2(buttonWidth - (hasScrollbar ? style.ScrollbarSize : 0.0f), 0)))
                    {
                        SelectedHoverGroupIndex = i;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        var addonNames = group.Elements.SelectMany(element => ElementUtil.GetAddonName(element)).ToArray();
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
            if (ImGui.Button(Language.HoverGroupsAddGroup))
            {
                var newGroup = new HoverGroup();
                Configuration.HoverGroups.Add(newGroup);
                SelectedHoverGroupIndex = Configuration.HoverGroups.Count - 1;
                Configuration.Save();
            }
        }


        ImGui.SameLine();

        // Right Pane: Group Details.
        using var rightChild = ImRaii.Child("HoverGroupDetails", Vector2.Zero, true);
        if (!rightChild.Success)
            return;

        if (SelectedHoverGroupIndex >= 0 && SelectedHoverGroupIndex < Configuration.HoverGroups.Count)
        {
            var selectedGroup = Configuration.HoverGroups[SelectedHoverGroupIndex];
            var groupName = selectedGroup.GroupName;
            if (ImGui.InputText("Group Name", ref groupName, 100))
            {
                selectedGroup.GroupName = groupName;
                Configuration.Save();
            }

            // right align delete button.
            var deleteButtonMargin = ImGui.GetContentRegionAvail().X - 15;
            ImGui.SameLine(deleteButtonMargin);
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.TrashAlt.ToIconString()}##{groupName}-delete"))
                {
                    Configuration.HoverGroups.RemoveAt(SelectedHoverGroupIndex);
                    SelectedHoverGroupIndex = -1;
                    Configuration.Save();
                }
            }
            ImGui.Separator();
            ImGui.TextUnformatted($"{Language.HoverGroupsElements}:");

            using var table = ImRaii.Table("ElementsTable", 2, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.Resizable);

            foreach (var element in ElementUtil.OrderedElements)
            {
                if (element.ShouldIgnoreElement())
                    continue;

                ImGui.TableNextColumn();
                var elementName = ElementUtil.GetElementName(element);
                var isInGroup = selectedGroup.Elements.Contains(element);

                if (ImGui.Checkbox(elementName, ref isInGroup))
                {
                    if (isInGroup)
                    {
                        if (!selectedGroup.Elements.Contains(element))
                            selectedGroup.Elements.Add(element);
                    }
                    else
                    {
                        selectedGroup.Elements.Remove(element);
                    }
                    Configuration.Save();
                }

                if (ImGui.IsItemHovered())
                {
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
        else
        {
            ImGui.TextUnformatted(Language.HoverGroupsTutorialHeader);
            Helper.BulletText(Language.HoverGroupsTutorialBody1);
            Helper.BulletText(Language.HoverGroupsTutorialBody2);
            Helper.BulletText($"{Language.HoverGroupsTutorialBody3}\n{Language.HoverGroupsTutorialBody4}");
        }
    }
}
