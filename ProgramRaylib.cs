using Chaos.UI.RaylibUI;
using System;

/// <summary>
/// Chaos: The Battle of Wizards — Raylib Graphical Frontend
///
/// Usage:  dotnet run [sprite_dir] [num_wizards] [num_humans]
///
/// Defaults: ./sprites, 2 wizards, 1 human
///
/// Controls:
///   Arrow keys    Move cursor / move unit one step
///   1-9           Select spell from list
///   0             Pass (no spell)
///   I             Cast as illusion
///   R             Cast for real
///   Space/Enter   Confirm target / move / attack
///   D             Done (finish this unit's movement)
///   N             Next creature (skip)
///   Escape        Cancel targeting
/// </summary>

string spritePath = args.Length > 0 ? args[0] : "./sprites";
int numWizards = Math.Clamp(args.Length > 1 ? int.Parse(args[1]) : 2, 2, 8);
int numHumans = Math.Clamp(args.Length > 2 ? int.Parse(args[2]) : 1, 0, numWizards);

var game = new RaylibGame(spritePath);
game.Run(numWizards, numHumans);
