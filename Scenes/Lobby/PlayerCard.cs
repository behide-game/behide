using System.Reactive.Subjects;
using Godot;
using Behide.UI.Controls;

namespace Behide.Game.UI.Lobby;

[SceneTree]
public partial class PlayerCard : BezelContainer
{
    private IDisposable? subscription;
    public int PeerId;

    public void BindPlayer(BehaviorSubject<Types.Player> player)
    {
        subscription = player.Subscribe(
            p =>
            {
                PeerId = p.PeerId;
                _.Margin.VBox.Username.Text = p.Username;
                _.Margin.VBox.Description.Visible = false;
            },
            onCompleted: QueueFree
        );
    }

    public void SetOwner(bool isOwner)
    {
        _.Margin.VBox.Description.Visible = isOwner;
        _.Margin.VBox.Description.Text = "Owner";
    }

    public override void _ExitTree() => subscription?.Dispose();
}
