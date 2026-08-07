using Godot;

namespace Behide.UI.Controls;

[Tool]
public partial class BezelContainer : Control
{
    [Export(PropertyHint.Range, "0, 100")]
    private int BezelSize { get; set { field = value; QueueRedraw(); } } = 45;

    [Export]
    public Color Color { get; set { field = value; QueueRedraw(); } } = Color.Color8(255, 255, 255);

    [Export]
    private int BorderWidth { get; set { field = value; QueueRedraw(); } }

    [Export]
    private Color BackgroundColor { get; set { field = value; QueueRedraw(); } } = Color.Color8(0, 0, 0);

    [ExportGroup("Corners")]
    [Export] public bool TopLeft { get; set { field = value; QueueRedraw(); } }
    [Export] public bool TopRight { get; set { field = value; QueueRedraw(); } } = true;
    [Export] public bool BottomRight { get; set { field = value; QueueRedraw(); } }
    [Export] public bool BottomLeft { get; set { field = value; QueueRedraw(); } } = true;

    public override void _Draw()
    {
        // Draw main shape
        var polygon = GetPolygonPoints(BezelSize, 0).ToArray();
        DrawColoredPolygon(polygon, Color);

        // If border width > 0, draw an inner shape
        if (BorderWidth <= 0) return;
        var offset = BorderWidth;

        // Draw the inner shape / the background
        var offsetBezelSize = OffsetBezelSize(BezelSize, offset);
        var innerPolygon = GetPolygonPoints(offsetBezelSize, offset).ToArray();
        DrawColoredPolygon(innerPolygon, BackgroundColor);
    }

    /// <summary>
    /// Offset the bezel sizes by a given amount
    /// </summary>
    /// <param name="bezelSizes">The bezel sizes</param>
    /// <param name="offset">The offset amount</param>
    private static float OffsetBezelSize(float bezelSizes, float offset)
    {
        const float c = 2 / Mathf.Sqrt2 - 2;
        return offset * c + bezelSizes;
    }

    /// <summary>
    /// Get the polygon points.
    /// The polygon is a rectangle with bezels in the corners.
    /// (It's a non-regular octagon)
    /// </summary>
    private IEnumerable<Vector2> GetPolygonPoints(float bezelSize, float offset)
    {
        var width = Size.X - offset;
        var height = Size.Y - offset;

        // Top left
        if (bezelSize > 0 && TopLeft)
        {
            yield return new Vector2(offset, bezelSize + offset);
            yield return new Vector2(bezelSize + offset, offset);
        }
        else yield return new Vector2(offset, offset);

        // Top right
        if (bezelSize > 0 && TopRight)
        {
            yield return new Vector2(width - bezelSize, offset);
            yield return new Vector2(width, bezelSize + offset);
        }
        else yield return new Vector2(width, offset);

        // Bottom right
        if (bezelSize > 0 && BottomRight)
        {
            yield return new Vector2(width, height - bezelSize);
            yield return new Vector2(width - bezelSize, height);
        }
        else yield return new Vector2(width, height);

        // Bottom left
        if (bezelSize > 0 && BottomLeft)
        {
            yield return new Vector2(bezelSize + offset, height);
            yield return new Vector2(offset, height - bezelSize);
        }
        else yield return new Vector2(offset, height);

        // Top left
        if (bezelSize > 0 && TopLeft)
        {
            yield return new Vector2(offset, bezelSize + offset);
            yield return new Vector2(bezelSize + offset, offset);
        }
        else yield return new Vector2(offset, offset);
    }
}
