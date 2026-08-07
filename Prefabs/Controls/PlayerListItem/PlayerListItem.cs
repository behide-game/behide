using Behide.Game.UI.Lobby;
using Godot;

namespace Behide.UI.Controls;

[SceneTree]
public partial class PlayerListItem : PlayerCard
{
    private Label UsernameLabel => _.Margin.HBox.Username;
    private TextureRect Logo => _.Margin.HBox.DeadIcon.TextureRect;

    public void SetPlayerName(string playerName) => UsernameLabel.Text = playerName;
    public void SetAlive(bool alive) => Logo.Modulate = Logo.Modulate with { A = alive ? 0f : 1f };
}
