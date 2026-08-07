using System.Reactive;
using System.Reactive.Subjects;
using Godot;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Video.__2_VBox Video => nodes.TabContainer.Video.VBox;
    private ConfigFile settingsOverride = new();

    private string renderingMethod = RenderingServer.GetCurrentRenderingMethod();
    private string renderingDriver = RenderingServer.GetCurrentRenderingDriverName();

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

    private void Video_SetVSyncMode(long mode)
    {
        DisplayServer.WindowSetVsyncMode(
        mode switch
        {
            1 => DisplayServer.VSyncMode.Enabled,
            2 => DisplayServer.VSyncMode.Adaptive,
            3 => DisplayServer.VSyncMode.Mailbox,
            _ => DisplayServer.VSyncMode.Disabled
        });
    }

    private void Video_SetDriver(long driver)
    {
        settingsOverride.Clear();
        settingsOverride.SetValue(
            "rendering",
            "renderer/rendering_method",
            driver == 2 ? "gl_compatibility" : "forward_plus"
        );

        if (driver == 0)
            settingsOverride.SetValue("rendering", "rendering_device/driver", "vulkan");
        else if (driver == 1)
            settingsOverride.SetValue("rendering", "rendering_device/driver.windows", "d3d12");

        string overridePath;
        if (OS.HasFeature("standalone"))
        {
            var exePath = OS.GetExecutablePath();
            var exeDir = Path.GetDirectoryName(exePath);
            if (exeDir is null)
            {
                log.Error("Cannot save settings override: Failed to retrieve executable directory from {ExePath}.", exePath);
                return;
            }

            overridePath = Path.Combine(exeDir, "override.cfg");
        }
        else
            overridePath = "./override.cfg";

        var err = settingsOverride.Save(overridePath);
        if (err != Error.Ok)
        {
            log.Error("Failed to save settings override: {Error}", err);
            return;
        }

        RefreshRestartNeeded();
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
        Video.Anti_aliasing.OptionButton.ItemSelected += Video_SetAntiAliasing;
        // VSync
        Video.VSync.OptionButton.ItemSelected += Video_SetVSyncMode;

        // FPS
        Video.FPS.Enabled.Toggled += GameManager.VisualEffectsLayer.EnableFpsDisplay;
        Video.MaxFPS.SliderSetting.Changed.Subscribe(maxFps => Engine.SetMaxFps((int)maxFps));

        // Driver
        Video.Driver.OptionButton.ItemSelected += Video_SetDriver;
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
        Video.Anti_aliasing.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
        Video.VSync.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
        Video.FPS.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Video.MaxFPS.SliderSetting.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
    }

    private void VideoApplyFromConfig(ConfigFile config)
    {
        var displayMode = config.GetValue(nameof(Video), "display-mode", "fullscreen").AsString();
        var uiScaling = config.GetValue(nameof(Video), "ui-scaling", 1).AsDouble();
        var renderScaleMode =
            renderingMethod is "forward_plus"
                ? config.GetValue(nameof(Video), "render-scale-mode", "normal").AsString()
                : "normal";

        var renderScale = config.GetValue(nameof(Video), "render-scale", 100).AsInt32();
        var antiAliasing = config.GetValue(nameof(Video), "anti-aliasing", "none").AsString();
        var vSync = config.GetValue(nameof(Video), "vsync", "disabled").AsString();
        var displayFps = config.GetValue(nameof(Video), "display-fps", false).AsBool();
        var maxFps = config.GetValue(nameof(Video), "max-fps", 0).AsInt32();

        Video.Driver.OptionButton.Select(renderingMethod switch
        {
            "gl_compatibility" => 2,
            _ => renderingDriver switch
            {
                "vulkan" => 0,
                "d3d12" => 1,
                _ => 0
            }
        });

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
        Video.Anti_aliasing.OptionButton.Select(antiAliasing switch
        {
            "none" => 0,
            "msaa2x" => 1,
            "msaa3x" => 2,
            "msaa4x" => 3,
            "smaa" => renderingMethod is "forward_plus" or "mobile" ? 4 : 0,
            "fxaa" => renderingMethod is "forward_plus" or "mobile" ? 5 : 0,
            "taa" => renderingMethod == "forward_plus" ? 6 : 0,
            _ => 0
        });

        Video.VSync.OptionButton.Select(vSync switch
        {
            "disabled" => 0,
            "enabled" => 1,
            "adaptive" => 2,
            "mailbox" => 3,
            _ => 0
        });

        Video.MaxFPS.SliderSetting.SetValue(maxFps);

        Video.Driver.OptionButton.SetItemDisabled(1, !OperatingSystem.IsWindows());
        Video.RenderScale.OptionButton.SetItemDisabled(1, renderingMethod is not "forward_plus" and not "mobile");
        Video.RenderScale.OptionButton.SetItemDisabled(2, renderingMethod is not "forward_plus" and not "mobile");
        Video.Anti_aliasing.OptionButton.SetItemDisabled(4, renderingMethod is "gl_compatibility");
        Video.Anti_aliasing.OptionButton.SetItemDisabled(5, renderingMethod is "gl_compatibility");
        Video.Anti_aliasing.OptionButton.SetItemDisabled(6, renderingMethod is not "forward_plus" and not "mobile");

        Video_SetDisplayMode(Video.DisplayMode.OptionButton.Selected);
        Video_SetUIScaling(Video.UIScaling.SliderSetting.Value);
        Video_SetRenderScaleMode(Video.RenderScale.OptionButton.Selected);
        Video_SetRenderScale(Video.RenderScale.SliderSetting.Value);
        Video_SetAntiAliasing(Video.Anti_aliasing.OptionButton.Selected);
        GameManager.VisualEffectsLayer.EnableFpsDisplay(displayFps);
        Engine.SetMaxFps(maxFps);
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
        config.SetValue(nameof(Video), "anti-aliasing", Video.Anti_aliasing.OptionButton.Selected switch
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
        config.SetValue(nameof(Video), "vsync", Video.VSync.OptionButton.Selected switch
        {
            0 => "disabled",
            1 => "enabled",
            2 => "adaptive",
            3 => "mailbox",
            _ => "disabled"
        });

        config.SetValue(nameof(Video), "display-fps", Video.FPS.Enabled.ButtonPressed);
        config.SetValue(nameof(Video), "max-fps", Video.MaxFPS.SliderSetting.Value);
    }
}
