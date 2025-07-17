using System;
using Godot;

namespace Behide.UI.Controls;

[GlobalClass, Tool]
public partial class StyleBoxBevel : StyleBox
{
    private int bevelSize;
    [Export] private int BevelSize { get => bevelSize; set { bevelSize = value; EmitChanged(); } }

    private Color color;
    [Export] private Color Color { get => color; set { color = value; EmitChanged(); } }

    public override void _Draw(Rid toCanvasItem, Rect2 rect)
    {
        ReadOnlySpan<Vector2> points = [
            // Top left
            new(rect.Position.X, rect.Position.Y),

            // Top right
            new(rect.End.X - bevelSize, rect.Position.Y),
            new(rect.End.X, rect.Position.Y + bevelSize),

            // Bottom right
            rect.End,

            // Bottom left
            new(rect.Position.X + bevelSize, rect.End.Y),
            new(rect.Position.X, rect.End.Y - bevelSize),

            // Top left
            rect.Position
        ];

        ReadOnlySpan<Color> colors = [color, color, color, color, color, color, color];

        RenderingServer.CanvasItemAddPolygon(toCanvasItem, points, colors, ReadOnlySpan<Vector2>.Empty, new Rid());
    }
}
