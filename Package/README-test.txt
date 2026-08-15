Welder Paint - internal test build
==================================

1. Make sure Space Engineers is CLOSED.
2. Run Install-TestPlugin.bat.
   - It finds Pulsar by itself (%AppData%\Pulsar).
   - If Pulsar is elsewhere, run it from a command line:
       Install-TestPlugin.bat "C:\path\to\Pulsar"
3. Start the game through Pulsar as usual.

How to test:
- Press P, pick a color/skin as usual (Apply Color / Apply Skin checkboxes work).
- Toggle paint mode with O (configurable in Pulsar plugin settings).
- Take a welder in hand, aim at any placed block, press LMB.
  Hold LMB to paint continuously.

IMPORTANT - do BOTH of these in the same world:
1. Paint a few blocks the VANILLA way (block in hand + P palette + touching
   the block with the ghost), both on a grid you own and one you do not own.
2. Paint the same blocks with the plugin (O + welder + LMB).
The log records every paint request from both paths, so we can compare
   exactly what vanilla sends vs what the plugin sends.

Expected behavior:
- Painted blocks change color (visual confirmation).
- NO 'server refused' message on grids you may paint (e.g. faction-shared).
   A refusal message should only appear on grids you really may not paint.
- On slow servers the paint can arrive with a delay (up to ~20 s) - that is normal,
   the plugin waits before reporting a problem.

To remove the plugin, run Uninstall-TestPlugin.bat.

If something does not work, send back the newest file:
%AppData%\SpaceEngineers\SpaceEngineers.log
(lines starting with [WelderPaint] tell us what happened).
