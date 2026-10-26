using UnityEngine;

public enum State { Idle, Walk, Jump, Attack, Hurt, Launched, Down, GetUp, Dead }

// 2.5D belt-scroller body. Logic runs in Step() at a fixed 60 Hz, in pixels and frames.
// Ground plane = (x, depth); height = jump height. Screen y = depth + height.
public class Fighter : MonoBehaviour
{
    const float Width = 16f;

    public int team;
    public int maxHealth = 5;
    public float walkX = Feel.WalkX, walkDepth = Feel.WalkDepth;
    public System.Action Think; // brain (Player or Enemy), runs before each tick

    public State state;
    public int stateFrames;
    public Vector2 ground, vel, moveInput;
    public float height, heightVel;
    public int health, facing = 1;
    public bool smash;
    public bool whip; // long reach, slow telegraphed wind-up

    int flashFrames, invulnFrames;
    bool bounced, strikePending, punchAlt;
    SpriteRenderer body, shadow, whipLine;
    Art.Frames art;

    int Startup => whip ? Feel.WhipStartup : Feel.PunchStartup;
    int Active => whip ? Feel.WhipActive : Feel.PunchActive;
    int Recovery => whip ? Feel.WhipRecovery : smash ? Feel.SmashRecovery : Feel.PunchRecovery;
    bool WindingUp => state == State.Attack && stateFrames < Startup;

    public bool CanAct => state == State.Idle || state == State.Walk;
    public bool Alive => health > 0;
    public bool Solid => Alive && height <= 0f && state != State.Launched && state != State.Down;
    public bool CanBeHit => Alive && invulnFrames == 0
        && state != State.Launched && state != State.Down && state != State.GetUp;

    public void Init(int team, string palette, Vector2 start)
    {
        this.team = team;
        ground = start;
        health = maxHealth;
        art = Art.For(palette);
        shadow = MakeSprite("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000; // under every body
        body = MakeSprite("Body", art.Idle[0]);
        whipLine = MakeSprite("Whip", Art.Whip);
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return sr;
    }

    public void Attack(bool isSmash)
    {
        if (!CanAct) return;
        smash = isSmash;
        punchAlt = !punchAlt;
        vel = Vector2.zero;
        Enter(State.Attack);
    }

    public void Jump()
    {
        if (!CanAct) return;
        heightVel = Feel.JumpVel;
        Enter(State.Jump);
    }

    public void TakeHit(int damage, int dir, bool launch, float push = Feel.JabPush)
    {
        if (!CanBeHit) return;
        bool countered = whip && WindingUp; // punished mid wind-up: knocked flying
        health -= damage;
        flashFrames = 8;
        facing = -dir;
        if (launch || countered || !Alive || height > 0f)
        {
            Enter(State.Launched);
            bounced = false;
            vel = new Vector2(dir * Feel.LaunchX, 0f);
            heightVel = Feel.LaunchUp;
        }
        else
        {
            Enter(State.Hurt);
            vel = new Vector2(dir * push, 0f);
        }
    }

    public bool InReach(Fighter t, float reach)
    {
        float dx = (t.ground.x - ground.x) * facing;
        return t.CanBeHit && dx > -4f && dx < reach
            && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand
            && Mathf.Abs(t.height - height) < Feel.HeightBand;
    }

    public void Step()
    {
        Think?.Invoke();
        Tick();
    }

    void Tick()
    {
        stateFrames++;
        if (flashFrames > 0) flashFrames--;
        if (invulnFrames > 0) invulnFrames--;

        bool landed = ApplyPhysics();
        switch (state)
        {
            case State.Idle:
            case State.Walk:
                vel = new Vector2(moveInput.x * walkX, moveInput.y * walkDepth);
                if (moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                state = vel == Vector2.zero ? State.Idle : State.Walk;
                break;
            case State.Jump:
                if (landed) Enter(State.Idle);
                break;
            case State.Attack:
                if (stateFrames == Startup) strikePending = true;
                if (stateFrames >= Startup + Active + Recovery) Enter(State.Idle);
                break;
            case State.Hurt:
                if (stateFrames >= Feel.Hitstun) Enter(State.Idle);
                break;
            case State.Launched:
                HitBodiesInPath();
                if (landed && bounced) Enter(State.Down);
                else if (landed) { bounced = true; heightVel = Feel.BounceUp; vel.x *= 0.5f; }
                break;
            case State.Down:
                if (stateFrames >= Feel.DownFrames) Enter(Alive ? State.GetUp : State.Dead);
                break;
            case State.GetUp:
                if (stateFrames >= Feel.GetUpFrames) { Enter(State.Idle); invulnFrames = Feel.GetUpInvuln; }
                break;
        }
    }

    // returns true on the frame the body touches the ground
    bool ApplyPhysics()
    {
        bool grounded = height <= 0f && heightVel <= 0f;
        if (grounded && !CanAct) vel.x = Mathf.MoveTowards(vel.x, 0f, Feel.Friction);
        ground += vel;
        ground.x = Mathf.Max(ground.x, Game.MinX);
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        if (grounded) return false;

        heightVel -= Feel.Gravity;
        height += heightVel;
        if (height > 0f) return false;
        height = 0f;
        heightVel = 0f;
        return true;
    }

    // called by Game after every fighter has stepped, so same-frame punches trade
    public void ResolveStrike()
    {
        if (!strikePending) return;
        strikePending = false;
        Strike();
    }

    void Strike()
    {
        bool hit = false;
        float reach = whip ? Feel.WhipReach : smash ? Feel.SmashReach : Feel.PunchReach;
        foreach (var t in Game.Fighters)
            if (t.team != team && InReach(t, reach))
            {
                t.TakeHit(smash ? 2 : 1, facing, smash, whip ? Feel.WhipPush : Feel.JabPush);
                hit = true;
            }
        if (hit) Game.Freeze(smash ? Feel.SmashHitstop : Feel.Hitstop, smash ? Feel.SmashShake : 0);
    }

    // a body flying fast knocks down its own teammates
    void HitBodiesInPath()
    {
        if (Mathf.Abs(vel.x) < Feel.BodyHitSpeed) return;
        int dir = vel.x > 0f ? 1 : -1;
        foreach (var o in Game.Fighters)
            if (o != this && o.team == team && o.CanBeHit
                && Mathf.Abs(o.ground.x - ground.x) < Width
                && Mathf.Abs(o.ground.y - ground.y) < Feel.DepthBand
                && Mathf.Abs(o.height - height) < Feel.HeightBand)
                o.TakeHit(1, dir, true);
    }

    void Enter(State s)
    {
        state = s;
        stateFrames = 0;
    }

    void LateUpdate()
    {
        if (body == null) return;
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);

        float h = Mathf.Round(height);
        bool striking = state == State.Attack && stateFrames >= Startup && stateFrames < Startup + Active + 6;
        body.sprite = state switch
        {
            State.Idle => art.Idle[Game.Frame / 10 % 4],
            State.Walk => art.Walk[Game.Frame / 8 % 4],
            State.Jump => art.Crouch,
            State.GetUp => art.GetUp,
            State.Attack when whip => WindingUp ? art.WindUp : striking ? art.Punch2 : art.Idle[0],
            State.Attack when smash => striking ? art.Kick : art.Crouch,
            State.Attack => striking ? (punchAlt ? art.Punch2 : art.Punch1) : art.Idle[0],
            State.Hurt => art.Hurt,
            _ => art.Fall, // launched, down, dead
        };
        body.flipX = facing < 0;
        body.transform.localPosition = new Vector3(0f, h, 0f);
        body.enabled = invulnFrames == 0 || invulnFrames / 3 % 2 == 0;
        bool telegraph = whip && WindingUp && stateFrames / 3 % 2 == 0;
        body.color = flashFrames > 0 && flashFrames / 2 % 2 == 0 ? new Color(1f, 0.45f, 0.45f)
            : telegraph ? new Color(1f, 1f, 0.45f)
            : Alive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

        whipLine.enabled = whip && striking;
        whipLine.transform.localScale = new Vector3(facing, 1f, 1f);
        whipLine.transform.localPosition = new Vector3(facing * 10f, h + 19f, 0f);

        // nearer to the camera (lower depth) draws on top; shadows under everything
        int order = -Mathf.RoundToInt(ground.y) * 2;
        body.sortingOrder = order;
        whipLine.sortingOrder = order + 1;
    }
}
