// Gameplay tuning. Units: pixels, pixels/frame, frames (60 fps).
// "guess" = placeholder until measured in Mesen; "measured" = taken from a RAM log of the original.
public static class Feel
{
    public const float WalkX = 1.5f;              // guess
    public const float WalkDepth = 1f;            // guess
    public const float JumpVel = 4.5f;            // guess
    public const float Gravity = 0.3f;            // guess
    public const float Friction = 0.25f;          // guess

    public const int PunchStartup = 3;            // guess
    public const int PunchActive = 2;             // guess
    public const int PunchRecovery = 8;           // guess
    public const int SmashRecovery = 16;          // guess
    public const int ComboWindow = 24;            // guess, frames between punch starts
    public const int InputBuffer = 8;             // guess
    public const float PunchReach = 22f;          // guess
    public const float SmashReach = 30f;          // guess
    public const float DepthBand = 6f;            // guess
    public const float HeightBand = 20f;          // guess
    public const float BodyGap = 12f;             // guess, closest two bodies can stand on one lane

    public const int Hitstop = 4;                 // guess
    public const int SmashHitstop = 10;           // guess
    public const int SmashShake = 8;              // guess
    public const int Hitstun = 16;                // guess
    public const float JabPush = 1.5f;            // guess
    public const float LaunchX = 5f;              // guess
    public const float LaunchUp = 4f;             // guess
    public const float BounceUp = 2f;             // guess
    public const float BodyHitSpeed = 2f;         // guess
    public const int DownFrames = 40;             // guess
    public const int GetUpFrames = 20;            // guess
    public const int GetUpInvuln = 30;            // guess

    public const int WhipStartup = 18;            // guess, telegraph window to dodge or counter
    public const int WhipActive = 4;              // guess
    public const int WhipRecovery = 22;           // guess
    public const float WhipReach = 52f;           // guess
    public const float WhipPush = 3f;             // guess
    public const int WhipCooldown = 90;           // guess

    public const float EnemyWalkX = 0.8f;         // guess
    public const int EnemyAttackCooldown = 70;    // guess
    public const int CorpseFrames = 60;
}
