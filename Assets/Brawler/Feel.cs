// Gameplay tuning. Units: pixels, pixels/frame, frames (60 fps). Pixel values sized for ~63 px tall fighters.
// "guess" = placeholder until measured in Mesen; "measured" = taken from a RAM log of the original.
public static class Feel
{
    public const float WalkX = 2f;              // guess
    public const float WalkDepth = 1.4f;            // guess
    public const float RunX = 4.5f;                 // guess
    public const int DoubleTapFrames = 15;        // guess, max gap between the two taps that start a run
    public const int FlyingKickStartup = 4;       // guess, crouch before take-off
    public const int FlyingKickActive = 24;       // guess, frames of flight
    public const int FlyingKickRecovery = 12;     // guess, drop and slide after
    public const float FlyingKickReach = 40f;     // guess, contact range ahead while flying
    public const float FlyingKickSpeed = 7f;      // guess, px/frame straight across the screen
    public const float FlyingKickHeight = 18f;    // guess, flies just above the ground
    public const int SpinKickStartup = 4;         // guess, crouch before the hop
    public const int SpinKickActive = 30;         // guess, frames of spinning
    public const int SpinKickRecovery = 12;       // guess, landing
    public const float SpinKickReach = 38f;       // guess, leg length on both sides while spinning
    public const float SpinKickSpeed = 5f;      // guess, px/frame travelled while spinning
    public const float SpinKickHeight = 20f;      // guess, hop height
    public const int SpinTurnFrames = 3;          // guess, frames per half turn
    public const float JumpVel = 6f;           // guess
    public const float Gravity = 0.3f;            // guess
    public const float Friction = 0.3f;          // guess

    public const int PunchStartup = 3;            // guess
    public const int PunchActive = 2;             // guess
    public const int PunchRecovery = 8;           // guess
    public const int SmashRecovery = 16;          // guess
    public const int ComboWindow = 24;            // guess, frames between punch starts
    public const int InputBuffer = 8;             // guess
    public const float PunchReach = 36f;          // guess
    public const float SmashReach = 44f;          // guess
    public const float DepthBand = 10f;            // guess
    public const float HeightBand = 36f;          // guess
    public const float BodyGap = 24f;             // guess, closest two bodies can stand on one lane

    public const int Hitstop = 4;                 // guess
    public const int SmashHitstop = 10;           // guess
    public const int SmashShake = 8;              // guess
    public const int FinisherHitstop = 20;        // guess, the last hit on an enemy
    public const int FinisherShake = 14;          // guess
    public const int FinisherFlash = 6;           // guess, frames of white screen flash
    public const float FinisherLaunch = 1.8f;     // guess, launch speed multiplier
    public const int Hitstun = 16;                // guess
    public const float JabPush = 2f;            // guess
    public const float LaunchX = 6f;              // guess
    public const float LaunchUp = 5f;             // guess
    public const float BounceUp = 2.5f;             // guess
    public const float BodyHitSpeed = 2.5f;         // guess
    public const int DownFrames = 40;             // guess
    public const int GetUpFrames = 20;            // guess
    public const int GetUpInvuln = 30;            // guess

    public const int KickStartup = 6;             // guess
    public const int KickActive = 3;              // guess
    public const int KickRecovery = 12;           // guess
    public const float KickReach = 46f;           // guess
    public const float KickPush = 4f;             // guess
    public const float JumpKickReach = 40f;       // guess
    public const float JumpKickHeight = 70f;      // guess, hits grounded targets from anywhere in the jump

    public const int PoundStartup = 8;            // guess, hammer stomp drives the target into the ground
    public const int PoundActive = 2;             // guess
    public const int PoundRecovery = 14;          // guess
    public const float PoundReach = 40f;          // guess
    public const int BuriedFrames = 100;          // guess, stuck in the ground before popping out
    public const float BurySink = 36f;            // guess, pixels below the ground

    public const float KneeDropHop = 3f;        // guess, small hop onto a downed enemy
    public const int KneeDropStartup = 20;        // ~ the hop's airtime: lands knee-first
    public const int KneeDropRecovery = 10;       // guess
    public const float KneeDropReach = 40f;       // guess

    public const int GrabFrames = 120;          // guess, how long a hold lasts
    public const int KneeStartup = 4;             // guess
    public const int KneeRecovery = 10;           // guess
    public const int ThrowStartup = 10;           // guess
    public const int ThrowRecovery = 16;          // guess

    public const int WhipStartup = 18;           // guess, telegraph window to dodge or counter
    public const int WhipActive = 4;              // guess
    public const int WhipRecovery = 22;           // guess
    public const float WhipReach = 80f;           // guess
    public const float WhipPush = 4f;             // guess
    public const int WhipCooldown = 90;           // guess

    public const float EnemyWalkX = 1.1f;         // guess
    public const int EnemyAttackCooldown = 70;    // guess
    public const int CorpseFrames = 60;
}
