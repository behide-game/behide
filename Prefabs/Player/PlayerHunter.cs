using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class PlayerHunter : PlayerBody
{

    public override void _EnterTree()
    {
        CameraDisk = _.Camera;
        camera = _.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        base._EnterTree();

        Health = 100;
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("suffer")) Health -= 10;
    }
}
