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
    private float paintRangeMeters = 15f;
    private bool requireWelder = true;
    private bool continuousPaint = true;
    private bool debugNetworkLogging = true;

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

    [Separator("Diagnostics")]

    [Checkbox(description: "Log all paint network traffic (outgoing, incoming, server rejections) to the game log as [WelderPaint][net] lines. Requires game restart after change.")]
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
