using Godot;

namespace Behide.UI.Controls;

[Tool, SceneTree]
public partial class PlayerListItem : Control
{
    private Label UsernameLabel => _.Autolayout.Container.MarginUsername.Username;
    private TextureRect Logo => _.Autolayout.MarginContainer.TextureRect;

    public void SetPlayerName(string playerName) => UsernameLabel.Text = playerName;
    public void SetStatus(bool status) {
        if(status)
        {
            Logo.Hide();
        }
        else
        {
            Logo.Show();
        }
    }
}
