namespace Behide.UI.Controls;

using Godot;

[Tool]
public partial class BezelContainer : Control
{
    private int _bezelSize = 45;
    private int _borderRadius = 10;
    private Color _color = Color.Color8(255, 255, 255);
    private bool _invertOrientation = false;

    [Export(PropertyHint.Range, "0, 100")] private int BezelSize { get => _bezelSize; set { _bezelSize = value; QueueRedraw(); } }
    [Export(PropertyHint.Range, "0, 100")] private int BorderRadius { get => _borderRadius; set { _borderRadius = value; QueueRedraw(); } }
    [Export] private Color Color { get => _color; set { _color = value; QueueRedraw(); } }
    [Export] private bool InvertOrientation { get => _invertOrientation; set { _invertOrientation = value; QueueRedraw(); } }

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var width = Size.X;
        var height = Size.Y;

        var topLeftBezelSize = !InvertOrientation ? BezelSize : BorderRadius;
        var topRightBezelSize = !InvertOrientation ? BorderRadius : BezelSize;

        var points = new Vector2[] {
            // Top left corner
            new(0, topRightBezelSize),
            new(topRightBezelSize, 0),

            // Top right corner
            new(width - topLeftBezelSize, 0),
            new(width, topLeftBezelSize),

            // Bottom right corner
            new(width, height - topRightBezelSize),
            new(width - topRightBezelSize, height),

            // Bottom left corner
            new(topLeftBezelSize, height),
            new(0, height - topLeftBezelSize)
        };

        DrawPolygon(points, [Color]);

        // Draw border radius in top left and bottom right corners
        Vector2 fstCirclePos;
        Vector2 sndCirclePos;

        if (!InvertOrientation)
        {
            fstCirclePos = new Vector2(BorderRadius, BorderRadius);
            sndCirclePos = new Vector2(width, height) - fstCirclePos;
        }
        else
        {
            fstCirclePos = new Vector2(width - BorderRadius, BorderRadius);
            sndCirclePos = new Vector2(BorderRadius, height - BorderRadius);
        }

        DrawCircle(fstCirclePos, BorderRadius, Color);
        DrawCircle(sndCirclePos, BorderRadius, Color);
    }
}
