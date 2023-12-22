#nullable disable
namespace Behide;

using Godot;
using Behide.Game.UI;

public partial class GameManager : Node3D
{
    public static UIManager Ui { get; private set; }

    public override void _EnterTree()
    {
        Ui = GetNode<UIManager>("/root/multiplayer/Managers/UIManager");
    }
}
