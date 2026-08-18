using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using ClientPlugin.Settings.Tools;
using VRage.Input;

namespace ClientPlugin;

public class Config : INotifyPropertyChanged
{
    #region Options

    private Binding paintKeybind = new Binding(MyKeys.O);
    private Binding eyedropperKeybind = new Binding(MyKeys.P, shift: true);
    private float paintRangeMeters = 15f;
    private bool requireWelder = true;
    private bool continuousPaint = true;
    private bool precisionTargeting = true;
    private bool debugNetworkLogging = false;

    #endregion

    #region User interface

    public readonly string Title = "Welder Paint";

    [Separator("Paint mode")]

    [Keybind(description: "Keybind that toggles welder paint mode. Unbind by right clicking the button.")]
    public Binding PaintKeybind
    {
        get => paintKeybind;
        set => SetField(ref paintKeybind, value);
    }

    [Keybind(description: "Eyedropper: copies the color and skin of the block under the crosshair into your selection (works while paint mode is on). Default Shift+P.")]
    public Binding EyedropperKeybind
    {
        get => eyedropperKeybind;
        set => SetField(ref eyedropperKeybind, value);
    }

    [Slider(2f, 100f, 1f, SliderAttribute.SliderType.Float, description: "Maximum distance (m) from the camera to the block being painted.")]
    public float PaintRangeMeters
    {
        get => paintRangeMeters;
        set => SetField(ref paintRangeMeters, value);
    }

    [Checkbox(description: "Only paint while actually holding a welder in hand.")]
    public bool RequireWelder
    {
        get => requireWelder;
        set => SetField(ref requireWelder, value);
    }

    [Checkbox(description: "Keep painting while the left mouse button is held down (spray painting). When off, each click paints one block.")]
    public bool ContinuousPaint
    {
        get => continuousPaint;
        set => SetField(ref continuousPaint, value);
    }

    [Checkbox(description: "Gun-style targeting: paint the first block whose visible model the crosshair ray crosses, then the collision hit, then the first occupied cell. Lets you aim at collision-less decorative blocks (corner lamps, pictures) as well as at the floor beside them. When off, any occupied cell the ray passes through is targeted.")]
    public bool PrecisionTargeting
    {
        get => precisionTargeting;
        set => SetField(ref precisionTargeting, value);
    }

    [Separator("Diagnostics")]

    [Checkbox(description: "Log all paint network traffic (outgoing, incoming, server rejections) to the game log as [WelderPaint][net] lines. Off by default; needs game restart after change.")]
    public bool DebugNetworkLogging
    {
        get => debugNetworkLogging;
        set => SetField(ref debugNetworkLogging, value);
    }

    #endregion

    #region Property change notification boilerplate

    public static readonly Config Default = new Config();
    public static readonly Config Current = ConfigStorage.Load();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
