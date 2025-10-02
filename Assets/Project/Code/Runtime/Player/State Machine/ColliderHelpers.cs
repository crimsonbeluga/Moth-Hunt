using UnityEngine;

// This is not part of PlayerState, it just sits in the same folder/assembly
public static class ColliderHelpers
{
    /// <summary>
    /// Forces the collider into the Stand profile.
    /// </summary>
    public static void ForceStand(SimpleCapsuleResizer col)
    {
        if (!col) return;
        col.Stand();
    }

    /// <summary>
    /// Forces the collider into the Crouch profile.
    /// </summary>
    public static void ForceCrouch(SimpleCapsuleResizer col)
    {
        if (!col) return;
        col.Crouch();
    }

    /// <summary>
    /// Forces the collider into the Glide profile.
    /// </summary>
    public static void ForceGlide(SimpleCapsuleResizer col)
    {
        if (!col) return;
        col.Glide();
    }
}
