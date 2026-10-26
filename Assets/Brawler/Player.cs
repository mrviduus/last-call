using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// WASD move, K / Space jump, J punch, L kick. Third punch of a quick combo = high kick. S + L = hammer stomp.
// J in the air = jump kick. Walk into a stunned enemy to grab: J = headbutt (third one throws), K = throw.
// Double-tap left/right = run; while running J = flying kick, L = bicycle kick.
// J next to a chair or bottle (nobody in reach) picks it up; then J = swing it, L = throw it.
// Jump into a pool lamp to hang on: A/D swing, J/L kick, K let go, S+J rip it down.
[RequireComponent(typeof(Fighter))]
public class Player : MonoBehaviour
{
    Fighter f;
    int combo, lastPunchFrame = -999, attackBuffer, jumpBuffer, kickBuffer, grabHits;
    int lastTapDir, lastTapFrame = -999, runRequested;
    bool attackPressed, jumpPressed, kickPressed;

    void Awake()
    {
        f = GetComponent<Fighter>();
        f.Think = Think;
    }

    // keys are polled every rendered frame and consumed by the next fixed step
    void Update()
    {
        attackPressed |= AttackPressed();
        jumpPressed |= JumpPressed();
        kickPressed |= KickPressed();

        int tap = TapDirection();
        if (tap != 0)
        {
            if (tap == lastTapDir && Game.Frame - lastTapFrame <= Feel.DoubleTapFrames) runRequested = tap;
            lastTapDir = tap;
            lastTapFrame = Game.Frame;
        }

        if (f.state == State.Dead && attackPressed) Game.RestartRequested = true;
        Game.ShowHelp = HelpHeld();
    }

    void Think()
    {
        f.moveInput = ReadMove();
        if (Game.Frame - lastTapFrame > Feel.InputBuffer) runRequested = 0; // stale double-tap (e.g. pumping a lamp)
        if (attackPressed) attackBuffer = Feel.InputBuffer;
        if (jumpPressed) jumpBuffer = Feel.InputBuffer;
        if (kickPressed) kickBuffer = Feel.InputBuffer;
        if (attackPressed || kickPressed) Game.Brawl = true; // the first swing starts the bar fight
        attackPressed = jumpPressed = kickPressed = false;

        if (f.state == State.Hang)
        {
            if (attackBuffer > 0 && f.moveInput.y < 0f) { f.RipLamp(); attackBuffer = 0; } // S+J: rip it down
            else if (attackBuffer > 0 || kickBuffer > 0) { f.HangKick(); attackBuffer = kickBuffer = 0; }
            else if (jumpBuffer > 0) { f.LetGo(); jumpBuffer = 0; }
        }
        else if (f.state == State.Grab)
        {
            if (attackBuffer > 0) { grabHits++; f.Attack(grabHits >= 3 ? Move.Throw : Move.Knee); attackBuffer = 0; }
            else if (jumpBuffer > 0) { f.Attack(Move.Throw); jumpBuffer = 0; }
        }
        else if (f.held != null && attackBuffer > 0 && f.CanAct)
        {
            f.Attack(Move.Swing); // holding a chair or bottle: swing it
            attackBuffer = 0;
        }
        else if (f.held != null && kickBuffer > 0 && f.CanAct)
        {
            f.Attack(Move.ThrowProp); // ...or throw it
            kickBuffer = 0;
        }
        else if (f.state == State.Jump && attackBuffer > 0)
        {
            f.JumpKick();
            attackBuffer = 0;
        }
        else if (f.state == State.Run && attackBuffer > 0)
        {
            f.Attack(Move.FlyingKick); // run + punch: long flying kick
            attackBuffer = 0;
        }
        else if (f.state == State.Run && kickBuffer > 0)
        {
            f.Attack(Move.BicycleKick); // run + kick: bicycle kick
            kickBuffer = 0;
        }
        else if (runRequested != 0 && f.CanAct)
        {
            f.StartRun(runRequested);
            runRequested = 0;
        }
        else if (attackBuffer > 0 && f.CanAct && f.IsDownedEnemyInReach(Feel.ElbowDropReach))
        {
            f.Attack(Move.ElbowDrop); // enemy lying in front: wrestling elbow drop
            attackBuffer = 0;
        }
        else if (kickBuffer > 0 && f.CanAct && f.moveInput.y < 0f)
        {
            f.Attack(Move.Pound); // down + kick = hammer stomp
            kickBuffer = 0;
        }
        else if (attackBuffer > 0 && f.CanAct && f.state != State.Run && f.held == null
                 && !f.EnemyInReach(Feel.PunchReach) && f.PropInReach() is Prop prop)
        {
            f.PickUp(prop); // nobody to hit, something to grab: pick it up
            attackBuffer = 0;
        }
        else if (attackBuffer > 0 && f.CanAct)
        {
            combo = Game.SimFrame - lastPunchFrame <= Feel.ComboWindow ? combo + 1 : 1;
            lastPunchFrame = Game.SimFrame;
            bool smash = combo >= 3;
            if (smash) combo = 0;
            f.Attack(smash ? Move.Smash : Move.Punch);
            attackBuffer = 0;
        }
        else if (kickBuffer > 0 && f.CanAct)
        {
            f.Attack(Move.Kick);
            kickBuffer = 0;
        }
        else if (jumpBuffer > 0 && f.CanAct)
        {
            f.Jump();
            jumpBuffer = 0;
        }
        else if (f.CanAct) TryGrab();

        if (attackBuffer > 0) attackBuffer--;
        if (jumpBuffer > 0) jumpBuffer--;
        if (kickBuffer > 0) kickBuffer--;
    }

    // walking into a stunned enemy grabs them
    void TryGrab()
    {
        if (f.moveInput.x == 0f || f.held != null) return; // hands full
        int dir = f.moveInput.x > 0f ? 1 : -1;
        foreach (var e in Game.Fighters)
        {
            float dx = (e.ground.x - f.ground.x) * dir;
            if (e.team != f.team && e.state == State.Hurt && e.CanBeHit && dx > 0f && dx <= Feel.BodyGap + 2f
                && Mathf.Abs(e.ground.y - f.ground.y) < Feel.DepthBand)
            {
                f.Grab(e);
                grabHits = 0;
                return;
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    static Vector2 ReadMove()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
        float y = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
        return new Vector2(x, y);
    }

    static bool JumpPressed() =>
        Keyboard.current != null && (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

    static bool AttackPressed() => Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
    static bool KickPressed() => Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
    static bool HelpHeld() => Keyboard.current != null && Keyboard.current.hKey.isPressed;

    static int TapDirection()
    {
        var k = Keyboard.current;
        if (k == null) return 0;
        return k.dKey.wasPressedThisFrame ? 1 : k.aKey.wasPressedThisFrame ? -1 : 0;
    }

#else
    static Vector2 ReadMove() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    static bool JumpPressed() => Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space);
    static bool AttackPressed() => Input.GetKeyDown(KeyCode.J);
    static bool KickPressed() => Input.GetKeyDown(KeyCode.L);
    static bool HelpHeld() => Input.GetKey(KeyCode.H);
    static int TapDirection() => Input.GetKeyDown(KeyCode.D) ? 1 : Input.GetKeyDown(KeyCode.A) ? -1 : 0;
#endif
}
