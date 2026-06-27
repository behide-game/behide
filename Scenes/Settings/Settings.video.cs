using Godot;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Video.__2_VBox Video => nodes.TabContainer.Video.VBox;

    private static (Vector2I res, string comment)[] GetResolutions()
    {
        var resolutions = new List<(Vector2I r, string c)>()
        {
            (new Vector2I(1280, 720), "16:9"),
            (new Vector2I(1440, 810), "16:9"),
            (new Vector2I(1600, 900), "16:9"),
            (new Vector2I(1920, 1080), "16:9"),
            (new Vector2I(2560, 1440), "16:9"),
            (new Vector2I(3840, 2160), "16:9")
        };

        DisplayServer.ScreenGetSize().Deconstruct(out var width, out var height);
        resolutions.Add((new Vector2I(width, height), "native"));
        resolutions.RemoveAll(r => r.r.X > width || r.r.Y > height);
        resolutions.Sort((a, b) => b.r.X.CompareTo(a.r.X));

        return resolutions.ToArray();
    }

    private void ListenVideoSettings()
    {
        // Windowing
        Video.WindowType.OptionButton.ItemSelected += selectedIdx =>
        {
            DisplayServer.WindowSetMode(
                selectedIdx switch
                {
                    0 => DisplayServer.WindowMode.ExclusiveFullscreen,
                    1 => DisplayServer.WindowMode.Windowed,
                    2 => DisplayServer.WindowMode.Fullscreen,
                    _ => DisplayServer.WindowMode.Windowed
                }
            );
        };

        // Resolutions
        // TODO: Update everytime the window changes screen
        var resolutions = GetResolutions();
        foreach (var (res, comment) in resolutions)
            Video.Resolution.OptionButton.AddItem($"{res.X}x{res.Y} ({comment})", resolutions.Length-1);

        Video.Resolution.OptionButton.ItemSelected += selectedIdx =>
            GetWindow().ContentScaleSize = resolutions[(int)selectedIdx].res;

        // UI Scaling
        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        Video.UIScaling.SliderSetting.Changed.Subscribe(scale =>
            GetWindow().ContentScaleFactor = (float)scale
        );

        // Render scale
        Video.RenderScale.OptionButton.ItemSelected += selectedIdx =>
            GetWindow().Scaling3DMode = selectedIdx switch
            {
                0 => Viewport.Scaling3DModeEnum.Nearest,
                1 => Viewport.Scaling3DModeEnum.Fsr,
                2 => Viewport.Scaling3DModeEnum.Fsr2,
                _ => Viewport.Scaling3DModeEnum.Nearest
            };
        Video.RenderScale.SliderSetting.Changed.Subscribe(scale =>
            GetWindow().Scaling3DScale = (float)scale / 100
        );

        // Anti aliasing
        Video.AntiAliasing.OptionButton.ItemSelected += selectedValue =>
        {
            GetWindow().Msaa3D = selectedValue switch
            {
                1 => Viewport.Msaa.Msaa2X,
                2 => Viewport.Msaa.Msaa4X,
                3 => Viewport.Msaa.Msaa8X,
                _ => Viewport.Msaa.Disabled,
            };
            GetWindow().ScreenSpaceAA = selectedValue switch
            {
                4 => Viewport.ScreenSpaceAAEnum.Smaa,
                5 => Viewport.ScreenSpaceAAEnum.Fxaa,
                _ => Viewport.ScreenSpaceAAEnum.Disabled
            };
            GetWindow().UseTaa = selectedValue == 6;
        };
    }
}
