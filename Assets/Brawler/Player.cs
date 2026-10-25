using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// WASD move, K / Space jump, J punch. Third punch of a quick combo = smash.
[RequireComponent(typeof(Fighter))]
public class Player : MonoBehaviour
{
    Fighter f;
    int combo, lastPunchFrame = -999, attackBuffer, jumpBuffer;
    bool attackPressed, jumpPressed;

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
        if (f.state == State.Dead && attackPressed) Game.RestartRequested = true;
        int stage = StagePressed();
        if (stage > 0) Game.RequestedStage = stage;
    }

    void Think()
    {
        f.moveInput = ReadMove();
        if (attackPressed) attackBuffer = Feel.InputBuffer;
        if (jumpPressed) jumpBuffer = Feel.InputBuffer;
        attackPressed = jumpPressed = false;

        if (attackBuffer > 0 && f.CanAct)
        {
            combo = Game.SimFrame - lastPunchFrame <= Feel.ComboWindow ? combo + 1 : 1;
            lastPunchFrame = Game.SimFrame;
            bool smash = combo >= 3;
            if (smash) combo = 0;
            f.Attack(smash);
            attackBuffer = 0;
        }
        else if (jumpBuffer > 0 && f.CanAct)
        {
            f.Jump();
            jumpBuffer = 0;
        }
        if (attackBuffer > 0) attackBuffer--;
        if (jumpBuffer > 0) jumpBuffer--;
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

    static int StagePressed()
    {
        var k = Keyboard.current;
        if (k == null) return 0;
        return k.digit1Key.wasPressedThisFrame ? 1 : k.digit2Key.wasPressedThisFrame ? 2 : 0;
    }
#else
    static int StagePressed() => Input.GetKeyDown(KeyCode.Alpha1) ? 1 : Input.GetKeyDown(KeyCode.Alpha2) ? 2 : 0;
    static Vector2 ReadMove() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    static bool JumpPressed() => Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space);
    static bool AttackPressed() => Input.GetKeyDown(KeyCode.J);
#endif
}
