using Godot;
using System.Globalization;

namespace Behide.UI.Controls;

[SceneTree("Slider_setting.tscn")]
public partial class SliderSetting : VBoxContainer
{
    private LineEdit LineEdit => _.Controls.LineEdit;
    private Slider Slider => _.Controls.Slider;

    public float Value => (float)Slider.Value;

    private void SliderValueChanged(float value) =>
        LineEdit.Text = value.ToString("0.00");

    private void LineEditLostFocus()
    {
        var text = LineEdit.Text;
        if (!double.TryParse(text, out var newValue))
        {
            LineEdit.Text = Value.ToString("0.00");
            return;
        }

        Slider.Value = newValue;
    }

    public override void _Input(InputEvent rawEvent)
    {
        if (rawEvent is not InputEventMouseButton mouseEvent) return;
        if (!mouseEvent.IsPressed()) return;
        if (mouseEvent.ButtonIndex != MouseButton.Left) return;

        var evLocal = (InputEventMouseButton)LineEdit.MakeInputLocal(mouseEvent);

        if (new Rect2(Vector2.Zero, LineEdit.Size).HasPoint(evLocal.Position)) return;
        LineEdit.ReleaseFocus();
    }
}
