
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerConfig : UdonSharpBehaviour
{
    public float jumpHeight = 4f;
    public float runSpeed = 4f;
    public float walkSpeed = 2f;
    public float strafeSpeed = 2f;
    public float gravity = 1f;

    void Start()
    {
        SetDefaults();
    }
    public void SetDefaults() 
    {
        if (Networking.LocalPlayer == null)
        {
            return;
        }
        Networking.LocalPlayer.SetJumpImpulse(jumpHeight);
        Networking.LocalPlayer.SetRunSpeed(runSpeed);
        Networking.LocalPlayer.SetWalkSpeed(walkSpeed);
        Networking.LocalPlayer.SetStrafeSpeed(strafeSpeed);
        Networking.LocalPlayer.SetGravityStrength(gravity);
    }
}