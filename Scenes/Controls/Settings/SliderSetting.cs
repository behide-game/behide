using Godot;
using System;
using System.Net.Mime;

[SceneTree("Slider_setting.tscn")]
public partial class SliderSetting : VBoxContainer
{
    private float Value => Convert.ToSingle(_.Controls.Slider.Value);

    // public override void _EnterTree()
    // {
    //     _.Controls.LineEdit.Text = Value.ToString();
    // }

    public float GetValue()
    {
        return Value;
    }

    private void OnValueChanged(float value)
    {
        _.Controls.LineEdit.Text = value.ToString();
    }

    private void OnTextChanged(float value)
    {
        _.Controls.Slider.Value = value;
    }
}
