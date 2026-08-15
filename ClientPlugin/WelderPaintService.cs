using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin;

/// <summary>
/// Paints the block under the crosshair with the player's currently selected color/skin
/// (from the P color picker), using the same server-validated request the vanilla
/// block-in-hand painting uses (MyCubeGrid.SkinBlocks).
/// </summary>
public class WelderPaintService : IDisposable
{
    private const int ContinuousPaintIntervalMs = 150;

    /// <summary>
    /// True while this service is issuing its own SkinBlocks call, so the debug
    /// network patches can mark the request as coming from the plugin, not vanilla.
    /// </summary>
    public static bool PluginRequestInProgress;

    // MyGuiScreenColorPicker is internal in Sandbox.Game -> resolve via reflection, never compile-time.
    private static readonly PropertyInfo ApplyColorProperty = AccessTools.Property(
        AccessTools.TypeByName("Sandbox.Game.Gui.MyGuiScreenColorPicker"), "ApplyColor");
    private static readonly PropertyInfo ApplySkinProperty = AccessTools.Property(
        AccessTools.TypeByName("Sandbox.Game.Gui.MyGuiScreenColorPicker"), "ApplySkin");

    private static readonly List<MyPhysics.HitInfo> Hits = new List<MyPhysics.HitInfo>();

    private readonly Config config = Config.Current;
    private bool paintMode;
    private int lastPaintMs;
    private bool welderMissingNotified;

    public void Update()
    {
        var session = MySession.Static;
        var input = MyInput.Static;
        if (session == null || input == null)
        {
            paintMode = false;
            return;
        }

        if (config.PaintKeybind.Key != MyKeys.None && !IsGuiFocused() && config.PaintKeybind.HasPressed(input))
        {
            paintMode = !paintMode;
            welderMissingNotified = false;
            Notify(paintMode ? "Welder paint: ON" : "Welder paint: OFF");
            Log(paintMode ? "paint mode ON" : "paint mode OFF");
            if (paintMode)
            {
                bool applyColor = (bool)ApplyColorProperty.GetValue(null);
                bool applySkin = (bool)ApplySkinProperty.GetValue(null);
                Log("palette state: applyColor=" + applyColor + " applySkin=" + applySkin
                    + " selectedHSV=" + MyPlayer.SelectedColor.ToString("F3")
                    + " slot=" + MyPlayer.SelectedColorSlot
                    + " skin='" + MyPlayer.SelectedArmorSkin + "'");
            }
        }

        ProcessPendingChecks();

        if (!paintMode || IsGuiFocused())
            return;

        var character = session.LocalCharacter;
        if (character == null || character.IsDead || character.Closed)
            return;

        if (config.RequireWelder && !(character.EquippedTool is MyWelder))
        {
            if (!welderMissingNotified)
            {
                welderMissingNotified = true;
                Notify("Welder paint: take a welder in hand");
            }
            return;
        }

        bool wantPaint = config.ContinuousPaint ? input.IsLeftMousePressed() : input.IsNewLeftMousePressed();
        if (!wantPaint)
            return;

        int now = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        if (now - lastPaintMs < ContinuousPaintIntervalMs)
            return;
        lastPaintMs = now;

        TryPaintTargetedBlock();
    }

    private void TryPaintTargetedBlock()
    {
        var camera = MySector.MainCamera;
        if (camera == null)
            return;

        Vector3D from = camera.Position;
        Vector3D direction = camera.WorldMatrix.Forward;
        Vector3D to = from + direction * config.PaintRangeMeters;

        Hits.Clear();
        MyPhysics.CastRay(from, to, Hits);

        foreach (var hit in Hits)
        {
            var hitEntity = hit.HkHitInfo.GetHitEntity();
            if (hitEntity == MySession.Static.LocalCharacter)
                continue;

            var grid = hitEntity as MyCubeGrid;
            if (grid == null && hitEntity != null)
                grid = FindParentGrid(hitEntity);

            if (grid == null)
            {
                Log("no grid on ray (hit " + (hitEntity == null ? "nothing" : hitEntity.GetType().Name) + ")");
                return; // Something else (voxel, ...) blocks the line of sight.
            }

            // Resolve the target block. RayCastBlocks() walks logical cells and returns the
            // first OCCUPIED one, so a visually small block (corner lamp, wall picture)
            // captures the whole cell and cannot be aimed past. In precision mode we instead
            // take the block the physics ray physically hit (real collision shape, like the
            // welder targets) by nudging the hit point slightly into the body and mapping
            // it to its cell. Non-precision mode and the fallback use the vanilla cell walk.
            Vector3I? cell = null;
            MySlimBlock slimBlock = null;
            bool usedCollisionTarget = false;
            if (config.PrecisionTargeting)
            {
                Vector3D hitPoint = hit.Position + direction * (grid.GridSize * 0.01f);
                Vector3I hitCell = grid.WorldToGridInteger(hitPoint);
                slimBlock = grid.GetCubeBlock(hitCell);
                if (slimBlock != null)
                {
                    cell = hitCell;
                    usedCollisionTarget = true;
                }
            }
            if (slimBlock == null)
            {
                cell = grid.RayCastBlocks(from, to);
                if (cell != null)
                    slimBlock = grid.GetCubeBlock(cell.Value);
            }
            if (slimBlock == null || cell == null)
            {
                Log("grid hit (" + hitEntity.GetType().Name + ") but no cube found within range");
                return;
            }

            bool applyColor = (bool)ApplyColorProperty.GetValue(null);
            bool applySkin = (bool)ApplySkinProperty.GetValue(null);
            Vector3? color = applyColor ? MyPlayer.SelectedColor : (Vector3?)null;
            MyStringHash? skin = applySkin
                ? MyStringHash.GetOrCompute(MyPlayer.SelectedArmorSkin)
                : (MyStringHash?)null;

            long myIdentity = MySession.Static.LocalHumanPlayer?.Identity.IdentityId ?? 0;
            Log("target " + slimBlock.BlockDefinition.Id + " at " + cell.Value
                + " grid " + grid.EntityId
                + " targeting=" + (usedCollisionTarget ? "collision" : "cell-walk")
                + " | applyColor=" + applyColor + " applySkin=" + applySkin
                + " | selectedHSV=" + (color.HasValue ? color.Value.ToString("F3") : "null")
                + " selectedSkin='" + (skin.HasValue ? skin.Value.String : "null") + "'"
                + " | blockHSV=" + slimBlock.ColorMaskHSV.ToString("F3")
                + " blockSkin='" + slimBlock.SkinSubtypeId.String + "'"
                + " | bigOwners=[" + string.Join(",", grid.BigOwners) + "]"
                + " myIdentity=" + myIdentity
                + " isServer=" + Sync.IsServer);

            if (color == null && skin == null)
            {
                Log("nothing to apply: enable Apply Color and/or Apply Skin in the P color picker");
                Notify("Welder paint: enable Apply Color/Skin in the P palette");
                return;
            }

            bool sameColor = color.HasValue && HsvEqual(slimBlock.ColorMaskHSV,
                ColorExtensions.UnpackHSVFromUint(color.Value.PackHSVToUint()));
            bool sameSkin = !skin.HasValue || skin.Value.String == slimBlock.SkinSubtypeId.String;
            if (sameColor && sameSkin)
            {
                // The server-side ChangeColorAndSkin() is a silent no-op in this case
                // (same HSV + same skin -> returns false, no sound, no notification).
                Log("skip: block already has exactly this color/skin");
                Notify("Welder paint: block already has this color/skin");
                return;
            }

            // Same request vanilla painting sends; validated on the server (ownership).
            PluginRequestInProgress = true;
            try
            {
                grid.SkinBlocks(cell.Value, cell.Value, color, skin, playSound: true);
            }
            finally
            {
                PluginRequestInProgress = false;
            }
            Log("request sent: " + slimBlock.BlockDefinition.Id + " at " + cell.Value);

            // Verify afterwards: SP applies synchronously, MP needs the OnSkinBlock
            // broadcast to come back (observed from ~50 ms up to ~16 s server lag),
            // so re-check every frame and only report failure after the deadline.
            for (int i = pendingChecks.Count - 1; i >= 0; i--)
            {
                if (pendingChecks[i].GridId == grid.EntityId && pendingChecks[i].Cell == cell.Value)
                    pendingChecks.RemoveAt(i); // dedupe while spray-painting one cell
            }
            pendingChecks.Add(new PendingCheck
            {
                GridId = grid.EntityId,
                Cell = cell.Value,
                SentHSV = color,
                // The wire format packs HSV through integers (PackHSVToUint), so the
                // applied value is NOT bitwise equal to the selected color.
                ExpectedHSV = color.HasValue
                    ? ColorExtensions.UnpackHSVFromUint(color.Value.PackHSVToUint())
                    : default(Vector3),
                SentSkin = skin.HasValue ? skin.Value.String : null,
                DeadlineMs = MySandboxGame.TotalGamePlayTimeInMilliseconds + (Sync.IsServer ? 0 : 20000),
            });
            return;
        }

        Log("no target within " + config.PaintRangeMeters + " m");
    }

    private struct PendingCheck
    {
        public long GridId;
        public Vector3I Cell;
        public Vector3? SentHSV;
        public Vector3 ExpectedHSV;
        public string SentSkin; // null = no skin requested
        public int DeadlineMs;
    }

    private readonly List<PendingCheck> pendingChecks = new List<PendingCheck>();

    private void ProcessPendingChecks()
    {
        if (pendingChecks.Count == 0)
            return;

        int now = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        for (int i = pendingChecks.Count - 1; i >= 0; i--)
        {
            var check = pendingChecks[i];

            var grid = MyEntities.GetEntityById(check.GridId) as MyCubeGrid;
            var block = grid?.GetCubeBlock(check.Cell);
            if (block == null)
            {
                pendingChecks.RemoveAt(i); // grid/block gone (destroyed, despawned)
                continue;
            }

            bool colorOk = !check.SentHSV.HasValue || HsvEqual(block.ColorMaskHSV, check.ExpectedHSV);
            bool skinOk = check.SentSkin == null || block.SkinSubtypeId.String == check.SentSkin;
            if (colorOk && skinOk)
            {
                pendingChecks.RemoveAt(i);
                Log("confirmed on grid " + check.GridId + " at " + check.Cell);
                continue;
            }

            if (now < check.DeadlineMs)
                continue; // server may still be lagging - keep waiting

            pendingChecks.RemoveAt(i);
            Log("REJECTED by server: grid " + check.GridId + " at " + check.Cell
                + " (color sent " + (check.SentHSV.HasValue ? check.SentHSV.Value.ToString("F3") : "n/a")
                + " expected(post-pack) " + (check.SentHSV.HasValue ? check.ExpectedHSV.ToString("F3") : "n/a")
                + " is " + block.ColorMaskHSV.ToString("F3")
                + ", owners [" + string.Join(",", grid.BigOwners) + "]"
                + ", myIdentity " + (MySession.Static.LocalHumanPlayer?.Identity?.IdentityId ?? 0) + ")");
            Notify("Welder paint: server refused - you cannot paint this grid");
        }
    }

    private static bool HsvEqual(Vector3 a, Vector3 b)
    {
        // HSV is packed to integers on the wire (hue resolution 1/360), so allow small slack.
        const float epsilon = 0.01f;
        return Math.Abs(a.X - b.X) <= epsilon && Math.Abs(a.Y - b.Y) <= epsilon && Math.Abs(a.Z - b.Z) <= epsilon;
    }

    private static MyCubeGrid FindParentGrid(IMyEntity entity)
    {
        var parent = entity.Parent;
        while (parent != null)
        {
            if (parent is MyCubeGrid grid)
                return grid;
            parent = parent.Parent;
        }
        return null;
    }

    private static bool IsGuiFocused()
    {
        // During normal gameplay the focused screen is MyGuiScreenGamePlay (the HUD/gameplay
        // screen) - the game itself uses this exact check (e.g. MyGridCameraSystem).
        return MyScreenManager.GetScreenWithFocus() is not MyGuiScreenGamePlay;
    }

    private static void Notify(string text)
    {
        MyHud.Notifications.Add(new MyHudNotification(MyStringId.GetOrCompute(text), 2000, "White"));
    }

    private static void Log(string message)
    {
        MyLog.Default.WriteLine("[WelderPaint] " + message);
    }

    public void Dispose()
    {
        paintMode = false;
        Hits.Clear();
    }
}
