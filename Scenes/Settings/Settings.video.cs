using System.Reactive;
using Godot;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Video.__2_VBox Video => nodes.TabContainer.Video.VBox;

    private void Video_SetDisplayMode(long displayMode) =>
        DisplayServer.WindowSetMode(
            displayMode switch
            {
                0 => DisplayServer.WindowMode.ExclusiveFullscreen,
                1 => DisplayServer.WindowMode.Windowed,
                2 => DisplayServer.WindowMode.Fullscreen,
                _ => DisplayServer.WindowMode.Windowed
            }
        );

    private void Video_SetUIScaling(double scale) =>
        GetWindow().ContentScaleFactor = (float)scale;

    private void Video_SetRenderScaleMode(long mode) =>
        GetWindow().Scaling3DMode = mode switch
        {
            0 => Viewport.Scaling3DModeEnum.Nearest,
            1 => Viewport.Scaling3DModeEnum.Fsr,
            2 => Viewport.Scaling3DModeEnum.Fsr2,
            _ => Viewport.Scaling3DModeEnum.Nearest
        };

    private void Video_SetRenderScale(double scale) =>
        GetWindow().Scaling3DScale = (float)scale / 100;

    private void Video_SetAntiAliasing(long mode)
    {
        GetWindow().Msaa3D = mode switch
        {
            1 => Viewport.Msaa.Msaa2X,
            2 => Viewport.Msaa.Msaa4X,
            3 => Viewport.Msaa.Msaa8X,
            _ => Viewport.Msaa.Disabled,
        };
        GetWindow().ScreenSpaceAA = mode switch
        {
            4 => Viewport.ScreenSpaceAAEnum.Smaa,
            5 => Viewport.ScreenSpaceAAEnum.Fxaa,
            _ => Viewport.ScreenSpaceAAEnum.Disabled
        };
        GetWindow().UseTaa = mode == 6;
    }


    private void VideoListenSettings()
    {
        // Display mode
        Video.DisplayMode.OptionButton.ItemSelected += Video_SetDisplayMode;

        // UI Scaling
        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        Video.UIScaling.SliderSetting.Changed.Subscribe(Video_SetUIScaling);

        // Render scale
        Video.RenderScale.OptionButton.ItemSelected += Video_SetRenderScaleMode;
        Video.RenderScale.SliderSetting.Changed.Subscribe(Video_SetRenderScale);

        // Anti-aliasing
        Video.AntiAliasing.OptionButton.ItemSelected += Video_SetAntiAliasing;

        // FPS
        Video.FPS.Enabled.Toggled += GameManager.VisualEffectsLayer.EnableFpsDisplay;
    }

    /// <summary>
    /// Trigger save when graphics settings changed
    /// </summary>
    private void VideoListenSettingsForSaving()
    {
        Video.DisplayMode.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
        Video.UIScaling.SliderSetting.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Video.RenderScale.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
        Video.RenderScale.SliderSetting.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Video.AntiAliasing.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
        Video.FPS.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
    }

    private void VideoApplyFromConfig(ConfigFile config)
    {
        var displayMode = config.GetValue(nameof(Video), "display-mode", "fullscreen").AsString();
        var uiScaling = config.GetValue(nameof(Video), "ui-scaling", 1).AsDouble();
        var renderScaleMode = config.GetValue(nameof(Video), "render-scale-mode", "normal").AsString();
        var renderScale = config.GetValue(nameof(Video), "render-scale", 100).AsInt32();
        var antiAliasing = config.GetValue(nameof(Video), "anti-aliasing", "none").AsString();
        var displayFps = config.GetValue(nameof(Video), "display-fps", false).AsBool();

        Video.DisplayMode.OptionButton.Select(displayMode switch
        {
            "fullscreen" => 0,
            "windowed" => 1,
            "borderless" => 2,
            _ => 0
        });
        Video.UIScaling.SliderSetting.SetValue(uiScaling);
        Video.RenderScale.OptionButton.Select(renderScaleMode switch
        {
            "normal" => 0,
            "fsr" => 1,
            "fsr2" => 2,
            _ => 0
        });
        Video.RenderScale.SliderSetting.SetValue(renderScale);
        Video.AntiAliasing.OptionButton.Select(antiAliasing switch
        {
            "none" => 0,
            "msaa2x" => 1,
            "msaa3x" => 2,
            "msaa4x" => 3,
            "smaa" => 4,
            "fxaa" => 5,
            "taa" => 6,
            _ => 0
        });

        Video_SetDisplayMode(Video.DisplayMode.OptionButton.Selected);
        Video_SetUIScaling(Video.UIScaling.SliderSetting.Value);
        Video_SetRenderScaleMode(Video.RenderScale.OptionButton.Selected);
        Video_SetRenderScale(Video.RenderScale.SliderSetting.Value);
        Video_SetAntiAliasing(Video.AntiAliasing.OptionButton.Selected);
        GameManager.VisualEffectsLayer.EnableFpsDisplay(displayFps);
    }

    private void VideoApplyToConfig(ConfigFile config)
    {
        config.SetValue(nameof(Video), "display-mode", Video.DisplayMode.OptionButton.Selected switch
        {
            0 => "fullscreen",
            1 => "windowed",
            2 => "borderless",
            _ => "fullscreen"
        });
        config.SetValue(nameof(Video), "ui-scaling", Video.UIScaling.SliderSetting.Value);
        config.SetValue(nameof(Video), "render-scale-mode", Video.RenderScale.OptionButton.Selected switch
        {
            0 => "normal",
            1 => "fsr",
            2 => "fsr2",
            _ => "normal"
        });
        config.SetValue(nameof(Video), "render-scale", Video.RenderScale.SliderSetting.Value);
        config.SetValue(nameof(Video), "anti-aliasing", Video.AntiAliasing.OptionButton.Selected switch
        {
            0 => "none",
            1 => "msaa2x",
            2 => "msaa3x",
            3 => "msaa4x",
            4 => "smaa",
            5 => "fxaa",
            6 => "taa",
            _ => "none"
        });
        config.SetValue(nameof(Video), "display-fps", Video.FPS.Enabled.ButtonPressed);
    }
}
