namespace VertigoShift.Gameplay.Gravity
{
    /// <summary>The four gravity surfaces of the cubic arena.</summary>
    public enum GravitySide
    {
        Floor = 0,     // gravity pulls -Y  (player stands on the floor)
        Ceiling = 1,   // gravity pulls +Y  (player stands on the ceiling)
        LeftWall = 2,  // gravity pulls -X  (player stands on the left wall)
        RightWall = 3  // gravity pulls +X  (player stands on the right wall)
    }
}
