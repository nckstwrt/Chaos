namespace Chaos.Models;

/// <summary>
/// A creature that has been summoned onto the board. Wraps CreatureStats
/// with runtime state: owner, position, whether it's an illusion, and
/// whether it has moved/attacked this turn.
///
/// In the original Z80 code, creatures are tracked in a flat array with
/// bitfield flags for status. Here we use a cleaner object model.
/// </summary>
public class BoardCreature
{
    public CreatureStats Stats { get; init; } = null!;

    /// <summary>ID of the wizard who owns this creature.</summary>
    public int OwnerWizardId { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>
    /// Illusions look and fight like real creatures, but are instantly
    /// destroyed by the Disbelieve spell. Any creature spell can be
    /// cast as an illusion with 100% success instead of rolling.
    /// </summary>
    public bool IsIllusion { get; set; }

    /// <summary>Has this creature already moved this turn?</summary>
    public bool HasMoved { get; set; }

    /// <summary>Has this creature already attacked this turn?</summary>
    public bool HasAttacked { get; set; }

    /// <summary>
    /// Is this creature currently engaged in melee (adjacent to an enemy)?
    /// Engaged creatures can only attack, not move freely.
    /// Recalculated each turn.
    /// </summary>
    public bool IsEngaged { get; set; }

    /// <summary>
    /// For spreading entities (Gooey Blob, Magic Fire): the turn counter.
    /// Spreads grow into adjacent squares each turn.
    /// </summary>
    public int SpreadTimer { get; set; }

    /// <summary>Reset movement/attack flags at the start of the owner's turn.</summary>
    public void ResetForNewTurn()
    {
        HasMoved = false;
        HasAttacked = false;
    }
}
