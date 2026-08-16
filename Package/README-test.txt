Welder Paint - internal test build v1.0.0
==========================================

This archive is password-protected (AES-256). Extract it with 7-Zip or
WinRAR using the password you received separately - Windows Explorer
cannot open AES-encrypted zips.

A plugin that lets you paint blocks by aiming a WELDER at them and pressing
the left mouse button - instead of the vanilla block-in-hand ghost painting.

1. Install
----------
1. Make sure Space Engineers is CLOSED.
2. Run Install-TestPlugin.bat.
   - It finds Pulsar by itself (%AppData%\Pulsar).
   - If Pulsar is elsewhere, run it from a command line:
       Install-TestPlugin.bat "C:\path\to\Pulsar"
3. Start the game through Pulsar as usual.
4. To remove the plugin later, run Uninstall-TestPlugin.bat.

1b. Manual install (if the .bat does not work)
----------------------------------------------
1. Win+R -> type %AppData%\Pulsar -> Enter. Use the Legacy or Interim
   folder matching the Pulsar edition you launch (if only one exists,
   use that one).
2. From the extracted Plugin\<edition>\ folder copy BOTH files
   (WelderPaint.dll and WelderPaint.dll.xml) into
   %AppData%\Pulsar\<edition>\Local\ (create the Local folder if missing).
   WHICH EDITION? Your game log's first lines show the runtime:
   ".NET 10.x" = use the Plugin\Interim (net10) files,
   ".NET Framework 4.x" = use Plugin\Legacy files.
   If unsure, copy the Interim files - current game builds are .NET 10.
3. Back up Profiles\Current.xml, then open it in Notepad and add
   <string>WelderPaint.dll</string> inside the <Local> section, e.g.:
       <Local>
         <string>WelderPaint.dll</string>
       </Local>
   If you find <Local /> on one line, replace that whole tag with the
   three lines above. Save (UTF-8).
4. Start the game through Pulsar.

2. How to test
--------------
Setup: enter a world (singleplayer or a server where you may build),
take a WELDER in hand, press P and pick any color.

Toggle paint mode with O. You should see "Welder paint: ON" on the HUD.
Aim at a block and press LMB. Hold LMB to paint continuously.
Press O again to turn paint mode off.

What to check (in any order):
a) Basic - paint a normal armor block: it changes color.
b) Precision on small blocks - place a corner floor lamp on a floor and
   a picture on a wall:
      - aiming AT the lamp/picture paints THE LAMP/PICTURE
      - aiming at the floor BESIDE the lamp paints the floor
      - aiming at the wall AROUND the picture paints the wall
   (same targeting feel as the welder's own highlight)
c) Skins - pick a skin in the P palette (Apply Skin checkbox): painting
   applies the skin, not just the color.
d) Server rules - on a multiplayer server, paint a grid you own or share
   with your faction: must work. On a grid you may NOT paint, you should
   see "server refused" (that message can appear with a delay on slow
   servers - up to ~20 s - this is normal).
e) Vanilla painting still works normally (block in hand + P + touching
   a block with the ghost).

3. Settings
-----------
Right after install the defaults are: keybind O, range 15 m, welder
required, continuous painting ON, precision targeting ON, debug network
logging ON. You can change them in the plugin's config (Pulsar settings).

4. If something does not work - or after the test
-------------------------------------------------
Run Collect-Diagnostics.bat (from the extracted folder). It collects all logs
and settings into WelderPaint-diagnostics-<date>.zip on your Desktop.
Send that one file back - nothing else needed.

Everything the plugin does is logged on lines starting with [WelderPaint].
Especially useful:
    [WelderPaint] paint mode ON / OFF
    [WelderPaint] target <block> at <cell> ... targeting=visual|collision|cell-walk
    [WelderPaint] confirmed on grid <id> at <cell>
    [WelderPaint] REJECTED by server: ...
    [WelderPaint][net] OUT/IN/SRV/REJ ...   (paint network traffic,
                                              also for vanilla painting)

Thank you for testing!
