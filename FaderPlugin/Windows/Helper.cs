using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FaderPlugin.Windows;

public static class Helper
{
    public static void TextColored(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(text);
    }

    public static void WrappedText(string text)
    {
        using (ImRaii.TextWrapPos(0.0f))
            ImGui.TextUnformatted(text);
    }

    public static void BulletText(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }

    public static void Tooltip(string tooltip)
    {
        using (ImRaii.Tooltip())
        using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
            ImGui.TextUnformatted(tooltip);
    }

    /// <summary>
    /// Displays a discrete slider widget for float values using an underlying integer slider.
    /// </summary>
    /// <param name="label">A unique label to identify the slider.</param>
    /// <param name="value">
    /// A reference to the float value. On update, the float is re-calculated as:
    /// value = (sliderValue * step) + min.
    /// </param>
    /// <param name="min">Minimum float value.</param>
    /// <param name="max">Maximum float value.</param>
    /// <param name="step">The increment size (discrete step), e.g. 10.0f for steps of 10.</param>
    /// <param name="format">A format string for displaying the value (e.g., "{0:0} ms").</param>
    /// <param name="itemWidth">Optional item width for the slider. Default is -1.</param>
    /// <returns>True if the user adjusted the slider, false otherwise.</returns>
    public static bool SliderFloatDiscrete(string label, ref float value, float min, float max, float step, string format, float itemWidth = -1)
    {
        var sliderMin = 0;
        var sliderMax = (int)((max - min) / step);
        var sliderValue = (int)((value - min) / step);

        var valueChanged = false;
        ImGui.SetNextItemWidth(itemWidth);

        // Create an integer slider with an empty format to hide the built-in display.
        if (ImGui.SliderInt($"{label}", ref sliderValue, sliderMin, sliderMax, ""))
        {
            // Convert the slider value back to the float value.
            value = sliderValue * step + min;
            valueChanged = true;
        }

        // Get the slider's bounding rectangle.
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var rectSize = rectMax - rectMin;
        // Calculate the center of the slider.
        var center = rectMin + rectSize * 0.5f;
        var text = string.Format(format, value);
        var textSize = ImGui.CalcTextSize(text);
        // Position the text so it is centered.
        var textPos = center - textSize * 0.5f;
        // Overlay the text.
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

        return valueChanged;
    }


}
