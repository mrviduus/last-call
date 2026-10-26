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
    public const int BicycleKickStartup = 4;      // guess, crouch before take-off
    public const int BicycleKickActive = 30;      // guess, frames of flying kicks
    public const int BicycleKickRecovery = 12;    // guess, landing
    public const float BicycleKickReach = 40f;    // guess
    public const float BicycleKickSpeed = 3.5f;   // guess, px/frame forward while kicking
    public const float BicycleKickHeight = 16f;   // guess, flies low
    public const int BicycleHitGap = 6;           // guess, frames between the alternating kicks
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

    public const float ElbowDropHop = 4.5f;       // guess, wrestling-style jump onto a downed enemy
    public const int ElbowDropStartup = 30;       // ~ the jump's airtime: lands elbow-first
    public const int ElbowDropRecovery = 14;      // guess, getting back up off the floor
    public const float ElbowDropReach = 40f;      // guess

    public const float PickupRange = 22f;         // guess, how close a prop must be to pick it up
    public const int SwingStartup = 6;            // guess, chair/bottle raised overhead
    public const int SwingActive = 3;             // guess
    public const int SwingRecovery = 14;          // guess
    public const float ChairReach = 46f;          // guess
    public const float BottleReach = 36f;         // guess
    public const int ChairHits = 3;               // guess, swings before a chair breaks
    public const int PropThrowStartup = 6;        // guess
    public const int PropThrowRecovery = 10;      // guess
    public const float PropThrowSpeed = 6f;       // guess
    public const float PropThrowUp = 2f;          // guess
    public const float CarrySpeed = 0.75f;        // guess, walking speed multiplier while holding a prop
    public const float TableHalfWidth = 20f;      // table footprint on the floor: blocks walking, can be jumped
    public const float TableHalfDepth = 7f;
    public const float TableHeight = 26f;         // jump higher than this to pass over a table
    public const float PoolHalfWidth = 54f;       // pool table footprint: heavy, never moves
    public const float PoolHalfDepth = 12f;
    public const float PoolHeight = 30f;          // its felt; balls and the cue rest on it
    public const float CueReach = 62f;            // guess, the longest reach in the game: a thrust
    public const int CueHits = 4;                 // guess
    public const int TableHits = 3;               // hits a tipped table takes before it breaks
    public const float TableSlide = 6f;           // guess, a tipped table hit again slides this fast
    public const float PropThrowHeight = 30f;     // guess, released at about hand height

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
    public const int SpawnGap = 40;               // guess, frames between enemies walking in
}
