using System.Reflection;
using ClientPlugin.Patches;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using VRage.Plugins;
using VRage.Utils;

// Define assembly version when compiled by Pulsar
#if !DEV_BUILD
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "WelderPaint";
    public static Plugin Instance { get; private set; }
    private SettingsGenerator settingsGenerator;
    private WelderPaintService service;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;
        Instance.settingsGenerator = new SettingsGenerator();
        service = new WelderPaintService();
        MyLog.Default.WriteLine("[WelderPaint] initialized v" + typeof(Plugin).Assembly.GetName().Version);

        if (Config.Current.DebugNetworkLogging)
        {
            var harmony = new Harmony(Name + ".netdebug");
            NetworkDebugPatches.Apply(harmony);
            MyLog.Default.WriteLine("[WelderPaint][net] network debug capture enabled");
        }
    }

    public void Dispose()
    {
        service?.Dispose();
        service = null;
        Instance = null;
    }

    public void Update()
    {
        service?.Update();
    }

    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        Instance.settingsGenerator.SetLayout<Simple>();
        MyGuiSandbox.AddScreen(Instance.settingsGenerator.Dialog);
    }
}
