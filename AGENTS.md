You are an experienced Space Engineers (version 1) client plugin developer.

Project: **Welder Paint** — a Pulsar client plugin that lets the player paint blocks by aiming a
welder at them, instead of the vanilla block-in-hand ghost painting.

Design constraints (do not break these):
- **No gameplay Harmony patches.** Runtime logic is public game API called from
  `Plugin.Update()` → `WelderPaintService`. Exception: `Patches/NetworkDebugPatches.cs` —
  diagnostic-only Harmony prefixes on the vanilla paint network path (`SkinBlocks`, `SkinGrid`,
  `SkinBlockRequest`, `OnSkinBlock`, `SkinGridFriendlyRequest`, `OnSkinGridFriendly`,
  `OnColorGridBlockFailed`), gated by `Config.DebugNetworkLogging`, installed in `Plugin.Init`.
  They only write `[WelderPaint][net]` log lines; never alter behavior.
- **Vanilla paint semantics:** color/skin come from `MyPlayer.SelectedColor` /
  `MyPlayer.SelectedArmorSkin` and the `MyGuiScreenColorPicker.ApplyColor/ApplySkin` checkboxes.
  Painting must go through `MyCubeGrid.SkinBlocks(min, max, hsv?, skin?, playSound)` — the same
  server-validated request vanilla uses (ownership checks stay on the server; never bypass them).
- Paint only the block under the crosshair, obtained via `MyPhysics.CastRay` from the
  camera, walking parents up to the `MyCubeGrid`. Default targeting is collision-precision:
  the physics hit point nudged 1% of `GridSize` into the body, mapped with
  `MyCubeGrid.WorldToGridInteger` (so visually small blocks like corner lamps / wall
  pictures can be aimed past). Fallback / non-precision mode: `MyCubeGrid.RayCastBlocks`
  + `GetCubeBlock` (vanilla whole-cell behavior).
- Never paint while a GUI screen has focus (`MyScreenManager.GetScreenWithFocus() != null` →
  actually `is not MyGuiScreenGamePlay`, the game's own gameplay-screen check).
- Eyedropper writes player selection via the public `MyPlayer.SelectedBuildColor` /
  `BuildArmorSkin` setters (same state the P color picker edits) - no network involved.
- C# `LangVersion=latest`, nullable disabled. Targets `net48` (Pulsar Legacy) and `net10.0`
  (Interim). No publicizer.

Key files:
- `ClientPlugin/Plugin.cs` — init + per-frame update.
- `ClientPlugin/WelderPaintService.cs` — keybind toggle, welder check, raycast targeting
  (visual AABB / collision / cell-walk), paint request, eyedropper (Shift+P), persistent
  paint-mode HUD notification (MyHudNotification with INFINITE lifespan), verification.
- `ClientPlugin/Config.cs` — settings dialog (keybinds, range, require-welder,
  continuous paint, precision targeting, debug logging).

Build: `dotnet build WelderPaint.sln` — deploys to `%AppData%\Pulsar\Legacy\Local` and
`...\Interim\Local`. The game must be CLOSED for the deploy copy to succeed.
Author/repo: dawidmachon/se-welder-paint (not yet pushed).

Internal test packages: `MakeTestPackage.bat [Debug|Release] [name] [expire-seconds]` —
assembles the build outputs + `Package\*` (install / uninstall scripts + tester README)
into `dist\<name>-<version>-test.zip`, **always AES-256 encrypted** (7-Zip, random
24-char password per build, saved to `dist\*.password.txt` — send zip link and
password over separate channels), then **auto-uploads** to tmpfiles.org
(`POST https://tmpfiles.org/api/v1/upload`, multipart `file` + `expire` seconds
defaulting to 21600; `UPLOAD=0` skips). Links land in `dist\*.upload.txt`; the direct
link is the page URL's `tmpfiles.org/` → `tmpfiles.org/dl/` variant. The tester
extracts with 7-Zip/WinRAR (Explorer cannot open AES zips) and runs
`Install-TestPlugin.bat`, which finds Pulsar (`%AppData%\Pulsar`), copies the files into
`Legacy\Local` / `Interim\Local` and enables the plugin in each edition's active profile
(`Profiles\Current.xml`, one-time `.bak-testplugin` backup, idempotent).
