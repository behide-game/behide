using Godot;

namespace Behide.UI.Controls;

[Tool, SceneTree]
public partial class PlayerListItem : Control
{
    private Label UsernameLabel => _.Container.MarginUsername.Username;
    private Label StatusLabel => _.Container.Ready.Label;

    public void SetPlayerName(string playerName) => UsernameLabel.Text = playerName;
    public void SetStatus(string status) => StatusLabel.Text = status;
}
