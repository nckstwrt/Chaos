namespace Chaos.Engine;

using Chaos.Models;

/// <summary>
/// Resolves combat between creatures and wizards. The original Chaos uses
/// a simple probabilistic system:
///
///   Attack roll:  random(0..7) + attacker's Combat
///   Defence roll: random(0..7) + defender's Defence
///   If attack > defence → defender is killed.
///
/// The same system applies to ranged attacks (using RangedCombat stat).
/// There's no health — everything dies in one successful hit.
///
/// "Engaged to engaged" combat has a modifier based on Manoeuvre:
/// if both attacker and target are engaged with enemies, the attacker
/// gets a bonus equal to (attacker.Manoeuvre - defender.Manoeuvre) / 2.
///
/// Magic attacks use a different system: attacker's spell power vs
/// defender's Magic Resistance, same random roll structure.
/// </summary>
public class CombatEngine
{
    private readonly GameState _game;

    public CombatEngine(GameState game)
    {
        _game = game;
    }

    /// <summary>
    /// Result of a combat attempt.
    /// </summary>
    public record CombatResult(
        bool AttackerWins,
        int AttackRoll,
        int DefenceRoll,
        string Description
    );

    /// <summary>
    /// Resolve a melee attack. The attacker and defender can be creatures
    /// or wizards — we just need their effective combat stats.
    /// </summary>
    public CombatResult ResolveMelee(
        int attackerCombat, int attackerManoeuvre,
        int defenderDefence, int defenderManoeuvre,
        bool bothEngaged)
    {
        int attackRoll  = _game.Rng.Next(10) + attackerCombat;
        int defenceRoll = _game.Rng.Next(10) + defenderDefence;

        // Manoeuvre modifier when both are engaged
        if (bothEngaged)
        {
            int manoeuvreBonus = (attackerManoeuvre - defenderManoeuvre) / 2;
            attackRoll += manoeuvreBonus;
        }

        bool wins = attackRoll > defenceRoll;
        string desc = $"Attack {attackRoll} vs Defence {defenceRoll}";
        return new CombatResult(wins, attackRoll, defenceRoll, desc);
    }

    /// <summary>
    /// Resolve a ranged attack. Same system as melee but using
    /// RangedCombat stat and no manoeuvre modifier.
    /// </summary>
    public CombatResult ResolveRanged(int attackerRangedCombat, int defenderDefence)
    {
        int attackRoll  = _game.Rng.Next(10) + attackerRangedCombat;
        int defenceRoll = _game.Rng.Next(10) + defenderDefence;

        bool wins = attackRoll > defenceRoll;
        string desc = $"Ranged {attackRoll} vs Defence {defenceRoll}";
        return new CombatResult(wins, attackRoll, defenceRoll, desc);
    }

    /// <summary>
    /// Resolve a magical attack (Lightning, Magic Bolt, etc.) against
    /// a target's Magic Resistance.
    /// </summary>
    public CombatResult ResolveMagicAttack(int spellPower, int defenderMagicResistance)
    {
        int attackRoll  = _game.Rng.Next(10) + spellPower;
        int defenceRoll = _game.Rng.Next(10) + defenderMagicResistance;

        bool wins = attackRoll > defenceRoll;
        string desc = $"Magic {attackRoll} vs Resist {defenceRoll}";
        return new CombatResult(wins, attackRoll, defenceRoll, desc);
    }

    /// <summary>
    /// Attempt to attack a target at (tx, ty) from (ax, ay).
    /// Handles all the lookup and consequence (killing creatures/wizards).
    /// Returns a description of what happened for the UI to display.
    /// </summary>
    public string ExecuteAttack(int ax, int ay, int tx, int ty, bool isRanged)
    {
        var targetCell = _game.Board[tx, ty];

        // Determine attacker stats
        var attackerWizard = _game.GetWizardAt(ax, ay);
        var attackerCreature = _game.GetCreatureAt(ax, ay);

        bool attackerIsUndead = false;
        int aCombat, aManoeuvre;
        if (attackerCreature != null)
        {
            aCombat = isRanged ? attackerCreature.Stats.RangedCombat : attackerCreature.Stats.Combat;
            aManoeuvre = attackerCreature.Stats.Manoeuvre;
            attackerIsUndead = attackerCreature.Stats.IsUndead;
        }
        else if (attackerWizard != null)
        {
            aCombat = isRanged ? attackerWizard.RangedCombat : attackerWizard.EffectiveCombat;
            aManoeuvre = attackerWizard.Manoeuvre;
        }
        else return "No attacker found.";

        // Determine defender stats
        int dDefence, dManoeuvre, dMagicRes;
        string targetName;
        bool targetIsWizard = false;
        bool targetIsUndead = false;
        Wizard? defenderWizard = null;
        BoardCreature? defenderCreature = null;

        if (targetCell.Content == Enums.CellContent.Wizard && targetCell.Wizard != null)
        {
            defenderWizard = targetCell.Wizard;
            dDefence = defenderWizard.EffectiveDefence;
            dManoeuvre = defenderWizard.Manoeuvre;
            dMagicRes = defenderWizard.MagicResistance;
            targetName = defenderWizard.Name;
            targetIsWizard = true;
        }
        else if (targetCell.Content == Enums.CellContent.Creature && targetCell.Creature != null)
        {
            defenderCreature = targetCell.Creature;
            dDefence = defenderCreature.Stats.Defence;
            dManoeuvre = defenderCreature.Stats.Manoeuvre;
            dMagicRes = defenderCreature.Stats.MagicResistance;
            targetName = defenderCreature.Stats.Name;
            targetIsUndead = defenderCreature.Stats.IsUndead;
        }
        else return "No target found.";

        // ── UNDEAD MELEE IMMUNITY ───────────────────────────────────
        // From Z80 at 0xB23D: if target is undead AND attacker is NOT
        // undead AND this is melee → attack is blocked.
        // Ranged attacks bypass this check (no undead check in ranged code).
        if (!isRanged && targetIsUndead && !attackerIsUndead)
        {
            return $"{targetName} is undead — cannot be attacked!";
        }

        // Resolve the combat
        CombatResult result;
        if (isRanged)
        {
            // Ranged creature attacks require line of sight
            if (!LineOfSight.HasClearPath(_game.Board, ax, ay, tx, ty))
                return "No line of sight to target.";

            result = ResolveRanged(aCombat, dMagicRes);
        }
        else
        {
            bool bothEngaged = _game.IsAdjacentToEnemy(ax, ay, -1)
                            && _game.IsAdjacentToEnemy(tx, ty, -1);
            result = ResolveMelee(aCombat, aManoeuvre, dDefence, dManoeuvre, bothEngaged);
        }

        // Apply result
        if (result.AttackerWins)
        {
            if (targetIsWizard && defenderWizard != null)
            {
                _game.KillWizard(defenderWizard);
                return $"{targetName} is destroyed! ({result.Description})";
            }
            else if (defenderCreature != null)
            {
                _game.RemoveCreature(defenderCreature);
                return $"{targetName} is killed! ({result.Description})";
            }
        }

        return $"{targetName} resists the attack. ({result.Description})";
    }
}
