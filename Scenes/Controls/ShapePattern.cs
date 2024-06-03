namespace Behide.UI.Controls;

using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class ShapePattern : Control
{
    private Texture2D _shape = new();
    [Export] private Texture2D Shape { get => _shape; set { _shape = value; QueueRedraw(); } }

    private float _shapeSize = 30;
    [Export(PropertyHint.Range, "0, 100")] private float ShapeSize { get => _shapeSize; set { _shapeSize = value; QueueRedraw(); } }

    private Vector2 _gap = Vector2.Zero;
    [Export] private Vector2 Gap { get => _gap; set { _gap = value; QueueRedraw(); } }

    private float _rotation = 0;
    [Export(PropertyHint.Range, "-360, 360")] private new float Rotation { get => _rotation; set { _rotation = value; QueueRedraw(); } }

    private Vector2 _offset = Vector2.Zero;
    [Export] private Vector2 Offset { get => _offset; set { _offset = value; QueueRedraw(); } }

    public override void _Draw()
    {
        if (Shape == null)
        {
            return;
        }

        // Calculate the size of the shape
        var rectSize = new Vector2(
            ShapeSize,
            ShapeSize / Shape.GetWidth() * Shape.GetHeight() // Keep the ratio of the initial shape
        );

        // Calculate the number of shapes to draw
        var diagonal = Mathf.Sqrt(Size.X * Size.X + Size.Y * Size.Y);
        var shapesPerColumn = Mathf.CeilToInt(diagonal / (rectSize.Y + Gap.Y));
        var shapesPerRow = Mathf.CeilToInt(diagonal / (rectSize.X + Gap.X));

        // Calculate the rotation
        var rotation = new Transform2D(
            Mathf.DegToRad(Rotation),
            new Vector2(
                shapesPerRow * (rectSize.X + Gap.X) / 2,
                shapesPerColumn * (rectSize.Y + Gap.Y) / 2
            )
        );

        // Draw the shapes
        for (var i = 0; i < shapesPerRow; i++) // Column
        {
            for (var j = 0; j < shapesPerColumn; j++) // Row
            {
                var pos = new Vector2(
                    i * (rectSize.X + Gap.X),
                    j * (rectSize.Y + Gap.Y)
                );
                pos *= rotation;
                pos += Size / 2;
                pos += Offset;

                var rect = new Rect2(
                    pos.X,
                    pos.Y,
                    rectSize.X,
                    rectSize.Y
                );
                DrawTextureRect(Shape, rect, false);
            }
        }
    }
}
