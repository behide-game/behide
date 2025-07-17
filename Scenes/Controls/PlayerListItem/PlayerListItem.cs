using Godot;
using Behide.Types;
// ReSharper disable InconsistentNaming

namespace Behide.UI.Controls;

[Tool]
public partial class PlayerListItem : Control
{
    private Player? _player;
    private Player? Player { get => _player; set { _player = value; Compute(); } }

    [Export] private Label usernameLabel = null!;
    [Export] private Label readyLabel = null!;

    private Serilog.ILogger log = null!;
    public override void _EnterTree() => log = Serilog.Log.ForContext("Tag", "UI/Lobby/PlayerListItem");

    public void SetPlayer(Player player)
    {
        if (player.State is not PlayerStateInLobby)
        {
            log.Error("Player state is not in lobby");
            return;
        }
        Player = player;
    }

    private void Compute()
    {
        if (Player is null) return;

        if (Player.State is not PlayerStateInLobby playerState)
        {
            log.Error("Player state is not in lobby");
            return;
        }

        usernameLabel.Text = Player.Username;
        readyLabel.Text = playerState.IsReady ? "Ready" : "Not ready";
    }

    public int? GetPeerId() => Player?.PeerId;
}
