using Godot;

namespace Behide.Game.Player.Prop;

[SceneTree("sound_button.tscn")]
public partial class SoundButton : Control
{
    public event Func<bool>? OnPressed;

    private string? audioName;
    private AudioStream? audioStream;
    private Tween? tween;

    public void SetAudio(string name, AudioStream stream)
    {
        audioName = name;
        audioStream = stream;
        _.Content.Margin.Label.Text = name;
        _.Content.Margin.Time.Text = stream.GetLength().ToString("0.#'s'");
        _.Content.Progress.OffsetTransformPositionRatio = new Vector2(1, 0);
        _.Button.Pressed += () =>
        {
            if (OnPressed?.Invoke() ?? false) StartAnimation();
        };
    }

    private void StartAnimation()
    {
        if (audioStream is null) return;

        _.Button.Disabled = true;
        _.Content.Progress.OffsetTransformPositionRatio = new Vector2(0, 0);

        tween?.Kill();
        tween = CreateTween().SetTrans(Tween.TransitionType.Linear);

        tween.TweenProperty(
            _.Content.Progress,
            Control.PropertyName.OffsetTransformPositionRatio.ToString(),
            new Vector2(1, 0),
            audioStream.GetLength()
        );
        tween.TweenCallback(Callable.From(() => _.Button.Disabled = false));
    }
}
