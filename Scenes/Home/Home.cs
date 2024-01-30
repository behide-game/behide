namespace Behide.Game.UI.Home;

using Godot;

public partial class Home : Control
{
    private void PlayButtonPressed()
    {
        GameManager.instance.SetGameState(GameManager.GameState.Lobby);
    }
}
