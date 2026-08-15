# Welder Paint

A [Pulsar](https://github.com/SpaceGT/Pulsar) client plugin for Space Engineers (v1) that lets you
**paint blocks with your welder** instead of the vanilla block-in-hand ghost painting.

## Why

In vanilla Space Engineers you repaint a block by taking *any* block in hand, opening the color
palette (P) and touching the side of an already placed block with the ghost. That is tedious for
small decorative blocks, where hitting the right side (or the right block at all) is fiddly.

This plugin keeps the normal color picker (P) for choosing the color and skin, but applies it by
simply **aiming a welder at a block and clicking** — much more precise, especially on small grids.

## How to use

1. Open the color palette with **P** and pick your color / skin (the *Apply Color* and
   *Apply Skin* checkboxes are respected, same as vanilla).
2. Toggle paint mode with the configurable keybind (default **O**).
3. Aim the crosshair at any placed block and press the left mouse button.
   Hold it to spray-paint continuously. With precision targeting the crosshair ray
   uses the block's real collision shape, so you can aim at the floor beside a corner
   lamp or the wall around a picture - exactly like welding does.

Server-side ownership rules are respected automatically — the plugin sends exactly the same
network request (`MyCubeGrid.SkinBlocks`) the vanilla ghost painting sends, so it works in
multiplayer without granting any extra rights.

## Settings (plugin config)

- **Paint keybind** — key (with optional Ctrl/Alt/Shift) that toggles paint mode, default `O`
- **Paint range** — max distance (m) from the camera to the target block, default 15 m
- **Require welder** — only paint while holding a welder (default on)
- **Continuous paint** — keep painting while LMB is held (default on)
- **Precision targeting** — target the block actually under the crosshair (real collision
  shape, like the welder), so visually small blocks that occupy a whole cube cell (corner
  lamps, wall pictures) can be aimed past (default on)
- **Debug network logging** — logs every paint network event (outgoing, incoming,
  server rejections) as `[WelderPaint][net]` lines in the game log, for both vanilla and
  plugin painting (default on, needs game restart after change)

Paint results are verified in the background: the plugin compares the block's color after the
server applied it (up to 20 s window, because MP servers can lag the paint broadcast) and tells
you on the HUD if a paint was actually refused.

## Build

`dotnet build WelderPaint.sln` — the DLL + descriptor are deployed to
`%AppData%\Pulsar\Legacy\Local` and `%AppData%\Pulsar\Interim\Local` (game must be closed).

Requires the .NET Framework 4.8.1 Developer Pack and the .NET 10 SDK.

## License

MIT © dawidmachon
