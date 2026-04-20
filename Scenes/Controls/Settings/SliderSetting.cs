using System.Reactive.Subjects;
using Godot;

namespace Behide.UI.Controls;

[SceneTree("Slider_setting.tscn")]
public partial class SliderSetting : VBoxContainer
{
    [Export] private string numberFormat = "0.00";

    private LineEdit LineEdit => _.Controls.LineEdit;
    private Slider Slider => _.Controls.Slider;

    public readonly Subject<double> Changed = new();
    public double Value => Slider.Value;

    public void SetValue(double value)
    {
        Slider.SetValueNoSignal(value);
        LineEdit.Text = value.ToString(numberFormat);
    }


    private void SliderValueChanged(double value)
    {
        LineEdit.Text = value.ToString(numberFormat);
        Changed.OnNext(value);
    }

    private void LineEditLostFocus()
    {
        var text = LineEdit.Text;
        if (!double.TryParse(text, out var newValue))
        {
            LineEdit.Text = Value.ToString(numberFormat);
            return;
        }

        Slider.Value = newValue;
        Changed.OnNext(newValue);
    }
}
