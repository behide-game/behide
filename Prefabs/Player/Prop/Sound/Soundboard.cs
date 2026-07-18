using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.Player.Prop;

[SceneTree("soundboard.tscn")]
public partial class Soundboard : Control
{
    [Export] private string[] audioNames = [];
    [Export] private AudioStream[] audioStreams = [];
    [Export] private PackedScene soundButtonScene = null!;
    [Export] private AudioStreamPlayer3D soundPlayer = null!;

    private readonly ILogger log = Log.CreateLogger(nameof(Soundboard));
    private VBoxContainer SoundsContainer => _.Margin.Container.VBox.Sounds;

    public override void _EnterTree()
    {
        Visible = false;
        if (!IsMultiplayerAuthority()) return;
        if (audioNames.Length != audioStreams.Length)
        {
            log.Error("Invalid amount of audio names or audio streams");
            return;
        }

        for (var i = 0; i < audioNames.Length; i++)
        {
            var soundButton = soundButtonScene.Instantiate<SoundButton>();
            soundButton.Name = i.ToString();
            soundButton.SetAudio(audioNames[i], audioStreams[i]);

            // Listen press
            var idx = i;
            soundButton.OnPressed += () => PlaySound(idx);

            SoundsContainer.AddChild(soundButton);
        }
    }

    private bool PlaySound(int index)
    {
        if (soundPlayer.IsPlaying()) return false;
        Rpc(nameof(PlaySoundRpc), index);
        return true;
    }

    [Rpc(CallLocal = true)]
    private void PlaySoundRpc(int index)
    {
        soundPlayer.Stream = audioStreams[index];
        soundPlayer.Play();
    }

    public new void SetVisible(bool visible)
    {
        Visible = visible;
        Input.MouseMode = visible
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;
    }
}
