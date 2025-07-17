using System;
using Godot;

namespace Behide.UI.Controls;

[GlobalClass]
public partial class AdvancedLabelCountdown : Countdown
{
    [Export] private Label[] labels = null!;
    [Export] private string[] runningTextFormats = null!;
    [Export] private string[] beforeRunningTexts = null!;
    [Export] private string[] afterRunningTexts = null!;

    protected override void Process(bool timeElapsed, TimeSpan remainingTime)
    {
        if (timeElapsed)
        {
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var afterRunningText = afterRunningTexts[i];
                label.SetText(afterRunningText);
            }
        }
        else
        {
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var runningTextFormat = runningTextFormats[i];
                label.SetText(string.Format(runningTextFormat, remainingTime));
            }
        }
    }
    protected override void Reset()
    {
        for (var i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            var afterRunningText = afterRunningTexts[i];
            label.SetText(afterRunningText);
        }
    }
}
