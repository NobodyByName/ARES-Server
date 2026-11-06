namespace Ares.Structs.Game.Movement;

/// <summary>Character posture and traversal state.</summary>
public enum CharacterStance : byte
{
    Stand = 0,
    Crouch = 1,
    Dbno = 2,
    Slide = 3,
    InAir = 4,
    Ladder = 5,
    Uncontrolled = 6,
}

/// <summary>Character locomotion intensity.</summary>
public enum LocoMode : byte
{
    Still = 0,
    Walking = 1,
    Running = 2,
    Sprinting = 3,
}
