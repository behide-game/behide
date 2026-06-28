using System.Reactive.Subjects;
using Godot;

namespace Behide.UI.Controls;

[SceneTree("SliderSetting.tscn")]
public partial class SliderSetting : BoxContainer
{
    [Export] private string numberFormat = "0.00";
    [Export] private bool fireChangedOnlyOnDragEnded;

    private LineEdit LineEdit => _.Controls.LineEdit;
    private Slider Slider => _.Controls.Slider;

    public readonly Subject<double> Changed = new();
    public double Value => Slider.Value;

    public override void _EnterTree() => LineEdit.Text = Slider.Value.ToString(numberFormat);

    public void SetValue(double value)
    {
        var clampedValue = Math.Clamp(value, Slider.MinValue, Slider.MaxValue);
        Slider.SetValueNoSignal(clampedValue);
        LineEdit.Text = clampedValue.ToString(numberFormat);
    }

    private void SliderDragEnded(bool valueChanged)
    {
        if (!valueChanged) return;
        LineEdit.Text = Slider.Value.ToString(numberFormat);
        Changed.OnNext(Slider.Value);
    }
    private void SliderValueChanged(double value)
    {
        LineEdit.Text = value.ToString(numberFormat);
        if (!fireChangedOnlyOnDragEnded) Changed.OnNext(value);
    }

    private void LineEditLostFocus() => SubmitLineEditText(LineEdit.Text);
    private void SubmitLineEditText(string text)
    {
        if (!double.TryParse(text, out var newValue))
        {
            LineEdit.Text = Value.ToString(numberFormat);
            return;
        }

        Slider.Value = newValue;
        Changed.OnNext(Slider.Value);
    }
}
