using Behide.OnlineServices.Signaling;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.UI.Home;

using Godot;

[SceneTree]
public partial class Home : Node3D
{
    private readonly ILogger log = Log.CreateLogger("UI/Home");

    private void SettingsButtonPressed()
    {
        GameManager.PauseMenu.Show();
        GameManager.PauseMenu.SwitchToSettingsMenu();
    }

    private async void QuitButtonPressed()
    {
        try
        {
            await GameManager.Room.LeaveRoom();
        }
        catch (Exception e)
        {
            log.Error(e, "Failed to quit Behide.");
        }
        finally
        {
            GetTree().Quit();
        }
    }

    private async void CreateRoomButtonPressed()
    {
        try
        {
            var roomId = await GameManager.Room.CreateRoom();
            log.Information("Room created with code {RoomId}", roomId);
            GameManager.SetGameState(GameManager.GameState.Lobby);
        }
        catch (Exception e)
        {
            log.Warning(e, "Trying to host game failed: {error}", e.Message); // TODO: Show error to user
        }
    }

    private void RoomCodeSubmitted(string _) => JoinRoomButtonPressed();
    private async void JoinRoomButtonPressed()
    {
        try
        {
            var lineEdit = _.UI.Content.Container.RoomButtons.Join.HBoxContainer.Input.Box.LineEdit;
            var rawCode = lineEdit.Text;
            var code = RoomId.tryParse(rawCode).ToNullable();
            if (code is null)
            {
                log.Error("Invalid room code");
                return;
            }

            await GameManager.Room.JoinRoom(code);
            GameManager.SetGameState(GameManager.GameState.Lobby);
        }
        catch (Exception e)
        {
            log.Warning("Trying to join room failed: {error}", e.Message); // TODO: Show error to user
        }
    }
}
