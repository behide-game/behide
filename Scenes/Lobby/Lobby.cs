namespace Behide.Game.UI.Lobby;

using Godot;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Serilog;

public partial class Lobby : Control
{
	private Control chooseModeControl = null!;
	private Control lobbyControl = null!;

	private ILogger Log = null!;

	public override void _EnterTree()
	{
		Log = Serilog.Log.ForContext("Tag", "UI/Lobby");

		chooseModeControl = GetNode<Control>("ChooseMode");
		lobbyControl = GetNode<Control>("Lobby");

		chooseModeControl.Visible = true;
		lobbyControl.Visible = false;
	}

	public override void _ExitTree()
	{
		GameManager.Room.PlayerRegistered -= OnPlayerRegistered;
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
		var res = await GameManager.Room.CreateRoom();

		res.Match(
			success: roomId => {
				Log.Information("Room created with code {RoomId}", RoomId.raw(roomId));
				ShowLobby(roomId);
			},
			failure: Log.Error
		);
	}

	private async void JoinButtonPressed()
	{
		var rawCode = GetNode<LineEdit>("ChooseMode/Buttons/Join/LineEdit").Text;
		var codeOpt = RoomId.tryParse(rawCode);

		if (codeOpt.HasValue(out var code) == false)
		{
			Log.Error("Invalid room code");
			return;
		}

		var res = await GameManager.Room.JoinRoom(code);

		res.Match(
			success: _ => {
				Log.Information("Joined room");
				ShowLobby(code);
			},
			failure: Log.Error
		);
	}


	private void OnPlayerRegistered(Behide.Player player)
	{
		var playerList = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players"); // TODO: Put this kind of thing in a variable editable in the Godot editor
		var playerLabel = new Label { Text = player.Username };
		playerList.AddChild(playerLabel);
	}
}
