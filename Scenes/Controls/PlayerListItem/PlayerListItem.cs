using Godot;
using Behide.Types;

namespace Behide.UI.Controls;

[Tool, SceneTree]
public partial class PlayerListItem : Control
{
    private Label UsernameLabel => _.Container.MarginUsername.Username;
    private Label ReadyLabel => _.Container.Ready.Label;
    private IDisposable? subscription;

    public void SetPlayer(IObservable<Player> player, Func<Player, string> label)
    {
        subscription = player.Subscribe(
            p =>
            {
                UsernameLabel.Text = p.Username;
                ReadyLabel.Text = label.Invoke(p);
            },
            onCompleted: QueueFree
        );
    }

    public override void _ExitTree() => subscription?.Dispose();
}
