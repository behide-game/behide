using System.Reactive.Subjects;
using Behide.Types;
using Godot;

namespace Behide.Game.UI.Lobby;

[SceneTree]
public partial class PlayerCard : Control
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
                _.Get().Modulate = p.State switch
                {
                    PlayerStateInLobby(IsReady: true) => Colors.White,
                    _ => Color.Color8(255, 255, 255, 155),
                };
            },
            onCompleted: QueueFree
        );
    }

    public void RefreshOwner(Room room)
    {
        _.Margin.VBox.Description.Visible = room.IsPeerOwner(PeerId);
        _.Margin.VBox.Description.Text = "Owner";
    }

    // Unsubscribe before the node is freed
    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        subscription?.Dispose();
    }
}
