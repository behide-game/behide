using Godot;

namespace Behide.Game.Player;

[SceneTree]
public partial class HealthBar : VBoxContainer
{
    private Label Label => _.HBox.Label;
    private Panel Bar => _.PanelContainer.Margin.Clip.Panel;
    [Export] private Color healthMaxColor;

    public void SetHealth(double health, int maxHealth)
    {
        Label.Text = ((int)Math.Ceiling(health * maxHealth)).ToString();
        Bar.OffsetTransformPositionRatio = new Vector2((float)health - 1, 0);

        const float redHueAngle = 0f;
        Bar.Modulate = Color.FromHsv(
            Mathf.LerpAngle(redHueAngle, healthMaxColor.H, (float)health),
            healthMaxColor.S,
            healthMaxColor.V
        );
    }
}
