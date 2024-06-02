namespace Behide.UI.Controls;

using System.Collections.Generic;
using System.Linq;
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

    private int _borderWidth = 0;
    private Color _backgroundColor = Color.Color8(0, 0, 0);
    [Export] private int BorderWidth { get => _borderWidth; set { _borderWidth = value; QueueRedraw(); } }
    [Export] private Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; QueueRedraw(); } }

    public override void _Draw()
    {
        var normalOrientation = !InvertOrientation;

        Vector4 bezelSizes = normalOrientation
            ? new(BorderRadius, BezelSize, BorderRadius, BezelSize)
            : new(BezelSize, BorderRadius, BezelSize, BorderRadius);

        // Draw main shape
        var polygon = GetPolygonPoints(bezelSizes, 0).ToArray();
        DrawColoredPolygon(polygon, Color);

        // Draw corner circles
        DrawCornerCircles(BorderRadius, 0, normalOrientation, Color);

        // If border width > 0, draw an inner shape
        if (BorderWidth > 0)
        {
            var offset = BorderWidth;

            // Draw inner circles
            var r = Mathf.Max(0, BorderRadius - offset);
            DrawCornerCircles(r, offset, normalOrientation, BackgroundColor);

            // Draw the inner shape / the background
            var offsetBezelSize = OffsetBezelSize(bezelSizes, offset);
            var correctedBezelSizes = new Vector4(            // Some bezels have the perfect size to fill the gap of a circle
                 normalOrientation ? r : offsetBezelSize.X,
                !normalOrientation ? r : offsetBezelSize.Y,
                 normalOrientation ? r : offsetBezelSize.Z,
                !normalOrientation ? r : offsetBezelSize.W
            );

            var innerPolygon = GetPolygonPoints(correctedBezelSizes, offset).ToArray();
            DrawColoredPolygon(innerPolygon, BackgroundColor);
        }
    }

    /// <summary>
    /// Draw circles at the top left and bottom right corners
    /// </summary>
    /// <param name="radius">The radius of the circles</param>
    /// <param name="normalOrientation">Draw circles at top right and bottom left if false</param>
    /// <param name="offset">The offset from the borders</param>
    /// <param name="color">The color of the circles</param>
    public void DrawCornerCircles(float radius, float offset, bool normalOrientation, Color color)
    {
        var width = Size.X - offset;
        var height = Size.Y - offset;

        Vector2 fstCirclePos = normalOrientation
            ? new(offset + radius, offset + radius)          // Top left
            : new(width - radius, offset + radius); // Top right

        Vector2 sndCirclePos = normalOrientation
            ? new(width - radius, height - radius) // Bottom right
            : new(offset + radius, height - radius);        // Bottom left

        DrawCircle(fstCirclePos, radius, color);
        DrawCircle(sndCirclePos, radius, color);
    }

    /// <summary>
    /// Offset the bezel sizes by a given amount
    /// </summary>
    /// <param name="bezelSizes">The bezel sizes</param>
    /// <param name="offset">The offset amount</param>
    public static Vector4 OffsetBezelSize(Vector4 bezelSizes, float offset)
    {
        const float c = 2 / Mathf.Sqrt2 - 2;
        float c2 = offset * c;

        return new Vector4(
            c2 + bezelSizes.X,
            c2 + bezelSizes.Y,
            c2 + bezelSizes.Z,
            c2 + bezelSizes.W
        );
    }

    /// <summary>
    /// Get the polygon points.
    /// The polygon is a rectangle with bezels in the corners.
    /// (It's an non regular octagon)
    /// </summary>
    public IEnumerable<Vector2> GetPolygonPoints(Vector4 bezelSizes, float offset)
    {
        var width = Size.X - offset;
        var height = Size.Y - offset;

        // Top left
        if (bezelSizes.X > 0)
        {
            yield return new(offset, bezelSizes.X + offset);
            yield return new(bezelSizes.X + offset, offset);
        }
        else yield return new(offset, offset);

        // Top right
        if (bezelSizes.Y > 0)
        {
            yield return new(width - bezelSizes.Y, offset);
            yield return new(width, bezelSizes.Y + offset);
        }
        else yield return new(width, offset);

        // Bottom right
        if (bezelSizes.Z > 0)
        {
            yield return new(width, height - bezelSizes.Z);
            yield return new(width - bezelSizes.Z, height);
        }
        else yield return new(width, height);

        // Bottom left
        if (bezelSizes.W > 0)
        {
            yield return new(bezelSizes.W + offset, height);
            yield return new(offset, height - bezelSizes.W);
        }
        else yield return new(offset, height);

        // Top left
        if (bezelSizes.X > 0)
        {
            yield return new(offset, bezelSizes.X + offset);
            yield return new(bezelSizes.X + offset, offset);
        }
        else yield return new(offset, offset);
    }
}
