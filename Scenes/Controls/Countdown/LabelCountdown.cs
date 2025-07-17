using System;
using Godot;

namespace Behide.UI.Controls;

[GlobalClass]
public partial class LabelCountdown : Countdown
{
    [Export] private Label label = null!;
    [Export] private string runningTextFormat = null!;
    [Export] private string beforeRunningText = null!;
    [Export] private string afterRunningText = null!;

    protected override void Process(bool timeElapsed, TimeSpan remainingTime)
    {
        label.SetText(
            timeElapsed
                ? afterRunningText
                : string.Format(runningTextFormat, remainingTime)
        );
    }
    protected override void Reset()
    {
        label.SetText(beforeRunningText);
    }
}
