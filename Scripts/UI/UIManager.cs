namespace Behide.Game.UI;

using Godot;

public partial class UIManager : Node3D
{
    [Signal]
    public delegate void TextLoggedEventHandler(string text);

    public void Log(string msg) {
        EmitSignal(SignalName.TextLogged, msg);
    }
}
