namespace Behide.UI.Controls;

using Godot;

[Tool]
public partial class PlayerListItem : Control
{
    private Player? _player;
    private Player? Player { get => _player; set { _player = value; Compute(); } }

    [Export] private string usernameLabelPath = "";
    [Export] private string readyLabelPath = "";

    public void SetPlayer(Player player) => Player = player;

    private void Compute()
    {
        if (Player is null) return;

        GetNode<Label>(usernameLabelPath).Text = Player.Username;
        GetNode<Label>(readyLabelPath).Text = Player.Ready ? "Ready" : "Not ready";
    }

    public string? GetUsername() => Player?.Username;
}
