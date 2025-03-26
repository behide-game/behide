namespace Behide.UI.Controls;

using Godot;
using Behide.Types;
using Serilog;


[Tool]
public partial class PlayerListItem : Control
{
    private Player? _player;
    private Player? Player { get => _player; set { _player = value; Compute(); } }

    [Export] private string usernameLabelPath = "";
    [Export] private string readyLabelPath = "";

    private ILogger Log = null!;
    public override void _EnterTree() => Log = Serilog.Log.ForContext("Tag", "UI/Lobby/PlayerListItem");

    public void SetPlayer(Player player)
    {
        if (player.State is not PlayerStateInLobby)
        {
            Log.Error("Player state is not in lobby");
            return;
        }
        Player = player;
    }

    private void Compute()
    {
        if (Player is null) return;

        if (Player.State is not PlayerStateInLobby playerState)
        {
            Log.Error("Player state is not in lobby");
            return;
        }

        GetNode<Label>(usernameLabelPath).Text = Player.Username;
        GetNode<Label>(readyLabelPath).Text = playerState.IsReady ? "Ready" : "Not ready";
    }

    public int? GetPeerId() => Player?.PeerId;
}
