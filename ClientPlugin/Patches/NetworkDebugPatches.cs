using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Patches;

/// <summary>
/// Debug-only instrumentation of the vanilla paint network path. Gated by Config.DebugNetworkLogging.
/// Captures, for BOTH vanilla ghost painting and this plugin:
///   OUT  MyCubeGrid.SkinBlocks / SkinGrid   - what leaves this machine (client side)
///   SRV  SkinBlockRequest / SkinGridFriendlyRequest - what the server received (host/SP only)
///   IN   OnSkinBlock / OnSkinGridFriendly   - what the server applied (broadcast, all machines)
///   REJ  OnColorGridBlockFailed             - the server rejected the paint (ownership etc.)
/// Everything lands in the game log as [WelderPaint][net] lines.
/// </summary>
public static class NetworkDebugPatches
{
    public static void Apply(Harmony harmony)
    {
        Patch(harmony, "SkinBlocks", nameof(SkinBlocksPrefix));
        Patch(harmony, "SkinGrid", nameof(SkinGridPrefix));
        Patch(harmony, "SkinBlockRequest", nameof(SkinBlockRequestPrefix));
        Patch(harmony, "OnSkinBlock", nameof(OnSkinBlockPrefix));
        Patch(harmony, "SkinGridFriendlyRequest", nameof(SkinGridFriendlyRequestPrefix));
        Patch(harmony, "OnSkinGridFriendly", nameof(OnSkinGridFriendlyPrefix));
        Patch(harmony, "OnColorGridBlockFailed", nameof(OnColorGridBlockFailedPrefix));
    }

    private static void Patch(Harmony harmony, string originalName, string prefixName)
    {
        var original = AccessTools.Method(typeof(MyCubeGrid), originalName);
        if (original == null)
        {
            MyLog.Default.WriteLine($"[WelderPaint][net] patch target missing: MyCubeGrid.{originalName} (game update?)");
            return;
        }
        var prefix = new HarmonyMethod(typeof(NetworkDebugPatches).GetMethod(prefixName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        harmony.Patch(original, prefix);
    }

    // OUT: public MyCubeGrid.SkinBlocks - called by vanilla MyCubeBuilder.Change AND by this plugin.
    private static void SkinBlocksPrefix(MyCubeGrid __instance, Vector3I min, Vector3I max, Vector3? newHSV, MyStringHash? newSkin, bool playSound)
    {
        var source = WelderPaintService.PluginRequestInProgress ? "PLUGIN" : "vanilla";
        MyLog.Default.WriteLine($"[WelderPaint][net] OUT SkinBlocks grid={__instance.EntityId} cells=[{min}..{max}] " +
            $"hsv={Hsv(newHSV)} skin={Skin(newSkin)} sound={playSound} src={source}");
    }

    // OUT: public MyCubeGrid.SkinGrid - vanilla whole-grid painting (Change with expand==-1).
    private static void SkinGridPrefix(MyCubeGrid __instance, Vector3 newHSV, MyStringHash newSkin, bool playSound, bool applyColor, bool applySkin)
    {
        var source = WelderPaintService.PluginRequestInProgress ? "PLUGIN" : "vanilla";
        MyLog.Default.WriteLine($"[WelderPaint][net] OUT SkinGrid grid={__instance.EntityId} hsv={Hsv(newHSV)} " +
            $"skin='{newSkin.String}' applyColor={applyColor} applySkin={applySkin} sound={playSound} src={source}");
    }

    // SRV: [Server] handler - fires on the server (visible in SP / when hosting).
    private static void SkinBlockRequestPrefix(MyCubeGrid __instance, Vector3I min, Vector3I max, MyCubeGrid.MyBlockVisuals visuals, bool playSound)
    {
        MyLog.Default.WriteLine($"[WelderPaint][net] SRV SkinBlockRequest grid={__instance.EntityId} cells=[{min}..{max}] " +
            $"{Visuals(visuals)} sound={playSound}");
    }

    // SRV: [Server] handler - fires on the server (visible in SP / when hosting).
    private static void SkinGridFriendlyRequestPrefix(MyCubeGrid __instance, MyCubeGrid.MyBlockVisuals visuals, bool playSound)
    {
        MyLog.Default.WriteLine($"[WelderPaint][net] SRV SkinGridFriendlyRequest grid={__instance.EntityId} {Visuals(visuals)} sound={playSound}");
    }

    // IN: [Broadcast] handler - fires on EVERY machine when the server applies a block paint.
    // If this appears in the log, the paint was accepted and replicated.
    private static void OnSkinBlockPrefix(MyCubeGrid __instance, Vector3I min, Vector3I max, MyCubeGrid.MyBlockVisuals visuals, bool playSound)
    {
        MyLog.Default.WriteLine($"[WelderPaint][net] IN  OnSkinBlock grid={__instance.EntityId} cells=[{min}..{max}] {Visuals(visuals)}");
    }

    // IN: [Broadcast] handler - whole-grid paint applied by the server.
    private static void OnSkinGridFriendlyPrefix(MyCubeGrid __instance, MyCubeGrid.MyBlockVisuals visuals, bool playSound)
    {
        MyLog.Default.WriteLine($"[WelderPaint][net] IN  OnSkinGridFriendly grid={__instance.EntityId} {Visuals(visuals)}");
    }

    // REJ: [Client] handler - the server refused the paint (e.g. ownership). Definitive failure signal.
    private static void OnColorGridBlockFailedPrefix(MyCubeGrid __instance)
    {
        MyLog.Default.WriteLine($"[WelderPaint][net] REJ OnColorGridBlockFailed grid={__instance.EntityId} owners=[{string.Join(",", __instance.BigOwners)}] " +
            $"myIdentity={(MySession.Static?.LocalHumanPlayer?.Identity?.IdentityId ?? 0)} - SERVER REFUSED this paint");
    }

    private static string Visuals(MyCubeGrid.MyBlockVisuals visuals)
    {
        return $"applyColor={visuals.ApplyColor} applySkin={visuals.ApplySkin} " +
            $"hsv={(visuals.ApplyColor ? Hsv(ColorExtensions.UnpackHSVFromUint(visuals.ColorMaskHSV)) : "n/a")} skin='{visuals.SkinId.String}'";
    }

    private static string Hsv(Vector3? hsv) => hsv.HasValue ? Hsv(hsv.Value) : "null";

    private static string Hsv(Vector3 hsv) => $"({hsv.X:F3},{hsv.Y:F3},{hsv.Z:F3})";

    private static string Skin(MyStringHash? skin) => $"'{(skin.HasValue ? skin.Value.String : "null")}'";
}
