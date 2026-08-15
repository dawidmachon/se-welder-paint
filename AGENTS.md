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
- Paint only the single cube under the crosshair (`MyCubeGrid.RayCastBlocks` + `GetCubeBlock`),
  obtained via `MyPhysics.CastRay` from the camera, walking parents up to the `MyCubeGrid`.
- Never paint while a GUI screen has focus (`MyScreenManager.GetScreenWithFocus() != null`).
- C# `LangVersion=latest`, nullable disabled. Targets `net48` (Pulsar Legacy) and `net10.0`
  (Interim). No publicizer.

Key files:
- `ClientPlugin/Plugin.cs` — init + per-frame update.
- `ClientPlugin/WelderPaintService.cs` — keybind toggle, welder check, raycast, paint request.
- `ClientPlugin/Config.cs` — settings dialog (keybind, range, require-welder, continuous paint).

Build: `dotnet build WelderPaint.sln` — deploys to `%AppData%\Pulsar\Legacy\Local` and
`...\Interim\Local`. The game must be CLOSED for the deploy copy to succeed.
Author/repo: dawidmachon/se-welder-paint (not yet pushed).

Internal test packages: `MakeTestPackage.bat [Debug|Release]` — assembles
`dist\WelderPaint-<version>-test.zip` from the build outputs + `Package\*` (install /
uninstall scripts + tester README). The tester just extracts the zip and runs
`Install-TestPlugin.bat`, which finds Pulsar (`%AppData%\Pulsar`), copies the files into
`Legacy\Local` / `Interim\Local` and enables the plugin in each edition's active profile
(`Profiles\Current.xml`, one-time `.bak-testplugin` backup, idempotent).
