namespace Behide.Game.UI.Lobby;

using Godot;
using Behide.OnlineServices;

public partial class Lobby : Control
{
    private Control chooseModeControl = null!;
    private Control lobbyControl = null!;

    public override void _EnterTree()
    {
        chooseModeControl = GetNode<Control>("ChooseMode");
        lobbyControl = GetNode<Control>("Lobby");

        chooseModeControl.Visible = true;
        lobbyControl.Visible = false;
    }

    private void ShowLobby(RoomId roomId)
    {
        chooseModeControl.Visible = false;
        lobbyControl.Visible = true;
        GetNode<Label>("Lobby/Header/Code/Value").Text = RoomId.raw(roomId);

        GameManager.Room.players.ForEach(OnPlayerRegistered);
        GameManager.Room.PlayerRegistered += OnPlayerRegistered;
    }


    private async void HostButtonPressed()
    {
        switch (await GameManager.Room.CreateRoom())
        {
            case Result<RoomId>.Error error:
                GameManager.Ui.LogError(error.Failure);
                return;

            case Result<RoomId>.Ok roomId:
                ShowLobby(roomId.Value);
                return;

            default:
                GameManager.Ui.LogError("Unexpected error");
                return;
        }
    }

    private void JoinButtonPressed()
    {
        var rawCode = GetNode<LineEdit>("ChooseMode/Buttons/Join/LineEdit").Text;
        var codeOpt = RoomId.tryParse(rawCode);
        var code = Option<RoomId>.ToNullable(codeOpt);

        if (code is null)
        {
            GameManager.Ui.LogError("Invalid room code");
            return;
        }

        GameManager.Room.JoinRoom(code);
        ShowLobby(code);
    }


    private void OnPlayerRegistered(Behide.Player player)
    {
        var playerList = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players"); // TODO: Put this kind of thing in a variable editable in the Godot editor
        var playerLabel = new Label { Text = player.Username };
        playerList.AddChild(playerLabel);
    }
}
