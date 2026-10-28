namespace Brawler
{
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

        public const int HeroHealth = 100;
        public const int GuyHealth = 40;              // a grunt dies to about one full combo
        public const int GirlHealth = 30;
        public const int PunchDamage = 7;             // the hero's jab
        public const int EnemyPunchDamage = 7;        // guess, an enemy's jab: about 15 to put the hero down
        public const int SmashDamage = 14;            // combo finisher
        public const int KickDamage = 12;
        public const int JumpKickDamage = 14;         // also the kick from a lamp
        public const int FlyingKickDamage = 24;
        public const int BicycleKickDamage = 6;       // per kick, about five land
        public const int PoundDamage = 14;
        public const int ElbowDropDamage = 20;
        public const int KneeDamage = 8;
        public const int ThrowDamage = 18;
        public const int WhipDamage = 8;
        public const int BodyHitDamage = 8;           // hit by a body flying past
        public const int LightPropDamage = 12;        // bottle, ball, cue
        public const int HeavyPropDamage = 18;        // chair, stool, lamp
        public const int ComboScaleFrom = 4;          // from this hit of a combo on, 10% less damage each (Streets of Rage 4)
        public const float ComboScaleMin = 0.5f;
        public const int JuggleCap = 8;               // hits in one combo before the victim falls whatever hit them

        public const int Hitstop = 6;                 // guess
        public const int SmashHitstop = 10;           // guess
        public const int SmashShake = 8;              // guess
        public const int FinisherHitstop = 20;        // guess, the last hit on an enemy
        public const int FinisherShake = 14;          // guess
        public const int FinisherFlash = 6;           // guess, frames of white screen flash
        public const float FinisherLaunch = 1.8f;     // guess, launch speed multiplier
        public const int Hitstun = 16;                // guess
        public const float HitJolt = 2f;              // px the victim shakes left-right during hitstop
        public const float WindUpLean = 2f;           // px leaning back while winding up
        public const float JabLunge = 3f;             // px forward on a jab's contact
        public const float HeavyLunge = 6f;           // ...on kicks, smashes, swings
        public const int SquashFrames = 5;            // squashed on landing, stretched on take-off
        public const int GhostGap = 3;                // frames between afterimages of a flying or bicycle kick
        public const int GhostFrames = 9;             // how long one afterimage fades
        public const int LastKillSlowMo = 40;         // rendered frames of slow motion on the last enemy's finisher
        public const float SlowMoScale = 0.3f;
        public const float JabPush = 2f;            // guess
        public const float LaunchX = 6f;              // guess
        public const float LaunchUp = 5f;             // guess
        public const float BounceUp = 2.5f;             // guess
        public const float BodyHitSpeed = 2.5f;         // guess
        public const int DownFrames = 70;             // guess, long enough to walk up and elbow-drop them
        public const int GetUpFrames = 20;            // guess
        public const int GetUpInvuln = 30;            // guess, the hero only: enemies are fair game the moment they stand
        public const int HeroHurtInvuln = 30;         // guess, hero can't be hit again right away (covers Hitstun)
        public const int BreakFreeInvuln = 20;        // guess, a released enemy can't be re-grabbed or hit at once

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
        public const float ElbowDropReach = 40f;      // guess, the landing hits this far ahead
        public const float ElbowDropHoming = 80f;     // guess, a body lying this far ahead (x) can be elbow-dropped: Brick steers onto it
        public const float ElbowDropHomingDepth = 24f; // ...and this far off his lane

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
        public const float LampBottom = 74f;          // shade rim height over the floor: high enough to jump up and grab
        public const float HangReach = 52f;           // rim to feet while hanging from it by the hands
        public const float LampGrabX = 18f;           // how close (x) to the swinging rim a jump has to pass
        public const float LampGrabDepth = 22f;       // ...and in depth (the pool table keeps you ~12 in front)
        public const float LampPump = 0.0015f;        // guess, swing added per frame of left/right input
        public const float LampGravity = 0.004f;      // guess, pendulum pull back to the middle
        public const float LampDamping = 0.995f;      // guess
        public const float LampMaxAngle = 0.4f;       // radians, the swing's limit
        public const int HangKickFrames = 16;         // guess, one kick from the lamp
        public const int HangKickStartup = 4;         // guess
        public const float HangKickReach = 46f;       // guess
        public const int LampHits = 2;                // a ripped-down lamp as a weapon: breaks on the second hit
        public const int TableHits = 3;               // hits a tipped table takes before it breaks
        public const float TableSlide = 6f;           // guess, a tipped table hit again slides this fast
        public const float PropThrowHeight = 30f;     // guess, released at about hand height

        public const int SpecialStartup = 4;          // J+K spinning kick all round
        public const int SpecialActive = 16;          // untouchable from the start to the end of this
        public const int SpecialRecovery = 16;
        public const float SpecialReach = 40f;        // both sides
        public const int SpecialDamage = 15;
        public const int SpecialCost = 8;             // health; can't be used with this little left
        public const int RecoverPerHit = 2;           // of the cost won back per enemy hit afterwards
        public const int SpecialLeniency = 3;         // frames J may come before K (or K before J) and still count as together

        // gore
        public const float NeckHeight = 48f;          // above the feet (a fighter is ~63 px, the head ~15)
        public const float ShoulderHeight = 42f;
        public const float HeadFlySpeed = 5f;         // a head knocked off by a killing kick
        public const float HeadFlyUp = 4f;
        public const float PuntSpeed = 6f;            // a head (or a ball) kicked along the floor
        public const float PuntUp = 3f;
        public const int HeadlessFrames = 60;         // a headless body stands this long, fountaining, before it drops
        public const int BleedFrames = 180;
        public const int HeadHits = 2;                // swings with a head before it bursts
        public const int ArmHits = 3;                 // ...with a torn-off arm
        public const int RipStartup = 8;
        public const int ArmRipDamage = 20;

        public const int GrabFrames = 120;          // guess, how long a hold lasts
        public const int KneeStartup = 4;             // guess
        public const int KneeRecovery = 10;           // guess
        public const int ThrowStartup = 10;           // guess
        public const int ThrowRecovery = 16;          // guess
        public const int LowBlowDamage = 10;          // J+L on a guy: knee where it hurts
        public const int DirtyCooldown = 180;         // guess, frames before J+L works again
        public const int LowBlowStun = 90;            // guess, frames he stays doubled over, open to anything
        public const int HairSlamStartup = 18;        // guess, lifting her up by the hair before the slam
        public const float HairSlamLift = 28f;        // how high she's lifted
        public const int HairSlamDamage = 22;         // J+L on a girl: by the hair, face-first into the floor

        public const int WhipStartup = 18;           // guess, telegraph window to dodge or counter
        public const int WhipActive = 4;              // guess
        public const int WhipRecovery = 22;           // guess
        public const float WhipReach = 80f;           // guess
        public const float WhipPush = 4f;             // guess
        public const int WhipCooldown = 90;           // guess

        public const float EnemyWalkX = 1.1f;         // guess
        public const int EnemyAttackCooldown = 70;    // guess
        public const int EnemyPunchStartup = 10;      // guess, a wind-up you can see (and punch them out of)
        public const int MaxAttackers = 2;            // enemies swinging at once; the rest wait their turn
        public const int MaxAttackersLate = 3;        // guess, ...from LateWave on and at midnight
        public const int LateWave = 3;                // guess
        public const float RingX = 70f;               // guess, how far off the hero (x) enemies wait their turn
        public const float RingDepth = 22f;           // guess, ...and off the hero's lane (depth), out of reach
        public const int EngageGiveUp = 180;          // guess, frames to reach the hero on our turn before passing it on
        public const int CorpseFrames = 60;
        public const int SpawnGap = 40;               // guess, frames between enemies walking in

        // physics: end walls, weight
        public const float WallSplatSpeed = 3f;       // guess, a body launched this fast bounces off the end wall
        public const int WallSplatDamage = 4;         // guess
        public const int WallBounceFrames = 20;       // guess, off the wall: open for one more hit this long
        public const float BossMass = 2f;             // flies half as far, pushes others twice as hard

        // the club's regulars
        public const int DrunkHealth = 50;
        public const float DrunkSway = 0.6f;          // guess, how much his walk wanders in depth
        public const int ChuckerHealth = 30;
        public const int ChuckerCooldown = 120;       // guess, frames between bottles
        public const float ChuckerRange = 110f;       // guess, keeps this far off the hero (x); a bottle flies ~134 px
        public const int BossHealth = 200;
        public const int BossHold = 30;               // guess, frames the boss holds the hero up before the throw
        public const float BossGrabChance = 0.35f;    // guess, of his turns he grabs instead of punching
        public const float GrudgeChance = 0.6f;       // guess, an enemy hit by a mate turns on them
        public const int GrudgeFrames = 5 * 60;       // guess, how long the grudge lasts before they remember Brick
        public const float WhiffCarry = 30f;          // guess, an enemy blow that misses the hero carries on this much further into a mate behind him
        public const float DrunkPickFight = 1f / 480f; // per frame OLD TOM picks a fight with a mate: about every 8 s
        public const float DrunkPickRange = 160f;     // ...with one this close

        // spilled drinks
        public const int PuddleFrames = 60 * 20;      // dries up after this long
        public const float PuddleHalfWidth = 12f;
        public const float PuddleHalfDepth = 5f;
        public const int SlipCooldown = 120;          // frames before the same fighter can slip again
        public const float EnemySlipChance = 0.02f;   // per frame an enemy walks through a puddle

        // molotov: a bottle Brick throws is lit; where it breaks the floor burns, and so does whoever stands in it
        public const int FireFrames = 4 * 60;
        public const float FireHalfWidth = 18f;
        public const float FireHalfDepth = 6f;
        public const int BurnFrames = 120;            // on fire this long after leaving the flames
        public const int BurnTick = 20;               // frames between burns
        public const int BurnDamage = 3;

        // last call: from the first punch the clock runs 11:59:00 to midnight in real seconds, then the boss
        public const int LastCallFrames = 60 * 60;
        public const int IntroFrames = 6 * 60;
        public const int ComboFrames = 90;            // the hero's combo counter resets after this long without a hit
    }
}
