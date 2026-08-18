Welder Paint - FIXED test build v1.1.0
=======================================

This build fixes the problem from the first package: your game runs on
.NET 10, but the first install placed the .NET Framework build of the
plugin. Pulsar showed it as enabled but it never actually started.

What to do (game CLOSED):
1. Extract this zip with 7-Zip or WinRAR (password sent separately).
2. Run Install-TestPlugin-Fixed.bat.
   It puts the correct .NET 10 build into all your Pulsar editions,
   removes the old files, and keeps the plugin enabled.
3. Start the game through Pulsar exactly like before.
4. Take a welder in hand, press P, pick a color, press O.
   You MUST see "Welder paint: ON" on the HUD.
5. Aim at a block, press LMB to paint. Hold LMB to spray.

Test checklist:
- normal block changes color
- corner floor lamp: aiming AT it paints the lamp, aiming at the floor
  BESIDE it paints the floor
- wall picture: aiming at it paints the picture, aiming at the wall
  around it paints the wall
- on a server: grids you own / your faction owns must paint; grids you
  may not paint show "server refused" (can be delayed up to ~20 s)
- vanilla painting (block in hand + P) still works normally

If it STILL does nothing after a minute of playing:
  run Collect-Diagnostics.bat from this folder and send back the
  WelderPaint-diagnostics zip it creates on your Desktop.

Thank you for testing!
