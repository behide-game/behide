namespace Behide.Game.UI;

using Godot;

public partial class LogStack : RichTextLabel
{
	public void LogMessage(string message) {
		AddText("\n" + message);
	}
}
