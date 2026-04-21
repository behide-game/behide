using Godot;

namespace Behide.Game.Supervisors;
using Types;

[SceneTree("../../../Prefabs/Supervisors/KillFieldItem.tscn")]
public partial class KillFieldItem : HBoxContainer
{
    private double stillDuration = 5.0;
    private double fadeDuration = 2.0;

    public override void _EnterTree()
    {
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.TweenInterval(stillDuration);
        tween.TweenProperty(this, "modulate", new Color(0xFFFFFF00), fadeDuration);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    public void SetKillerAndKilled(Player killer, Player killed)
    {
        _.Cause.SetVisible(true);
        _.Killer.SetVisible(true);
        _.Killed.SetVisible(true);
        _.Killer.Text = killer.Username;
        _.Killed.Text = killed.Username;
    }

    public void SetKilledThemself(Player killed)
    {
        _.Cause.SetVisible(true);
        _.Killer.SetVisible(true);
        _.Killed.SetVisible(false);
        _.Killer.Text = killed.Username;
        _.Cause.Text = "killed himself";
    }
}
