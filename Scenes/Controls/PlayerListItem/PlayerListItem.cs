using Godot;
using Behide.Types;

namespace Behide.UI.Controls;

[Tool, SceneTree]
public partial class PlayerListItem : Control
{
    private Label UsernameLabel => _.Container.MarginUsername.Username;
    private Label ReadyLabel => _.Container.MarginReady.Ready.Label;
    private IDisposable? subscription;

    public void SetPlayer(IObservable<Player> player)
    {
        subscription = player.Subscribe(
            p =>
            {
                UsernameLabel.Text = p.Username;
                ReadyLabel.Text =
                    p.State is PlayerStateInLobby isReady
                        ? isReady.IsReady ? "Ready" : "Not ready"
                        : "Gone";
            },
            onCompleted: QueueFree
        );
    }

    public override void _ExitTree() => subscription?.Dispose();
}
