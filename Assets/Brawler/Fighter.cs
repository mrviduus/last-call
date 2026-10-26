using UnityEngine;

public enum State { Idle, Walk, Run, Jump, Attack, Hurt, Launched, Down, GetUp, Dead, Grab, Held, Buried }
public enum Move { Punch, Smash, Kick, JumpKick, SpinKick, Pound, KneeDrop, Knee, Throw, Whip }

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
    public Move move;
    public bool whip; // enemy type: long reach, slow telegraphed wind-up
    public Fighter grabbed;

    int flashFrames, invulnFrames;
    bool bounced, strikePending, punchAlt, airKick, airKickSpent;
    SpriteRenderer body, shadow, whipLine, boot;
    Art.Frames art;

    int Startup => move switch
    {
        Move.Kick => Feel.KickStartup, Move.SpinKick => Feel.SpinKickStartup,
        Move.Pound => Feel.PoundStartup, Move.KneeDrop => Feel.KneeDropStartup, Move.Knee => Feel.KneeStartup,
        Move.Throw => Feel.ThrowStartup, Move.Whip => Feel.WhipStartup, _ => Feel.PunchStartup,
    };
    int Active => move switch
    {
        Move.Kick => Feel.KickActive, Move.SpinKick => Feel.SpinKickActive, Move.Whip => Feel.WhipActive, _ => Feel.PunchActive,
    };
    int Recovery => move switch
    {
        Move.Smash => Feel.SmashRecovery, Move.Kick => Feel.KickRecovery,
        Move.SpinKick => Feel.SpinKickRecovery, Move.Pound => Feel.PoundRecovery,
        Move.KneeDrop => Feel.KneeDropRecovery, Move.Knee => Feel.KneeRecovery,
        Move.Throw => Feel.ThrowRecovery, Move.Whip => Feel.WhipRecovery, _ => Feel.PunchRecovery,
    };
    float Reach => move switch
    {
        Move.Smash => Feel.SmashReach, Move.Kick => Feel.KickReach,
        Move.JumpKick => Feel.JumpKickReach, Move.SpinKick => Feel.SpinKickReach, Move.Pound => Feel.PoundReach,
        Move.KneeDrop => Feel.KneeDropReach,
        Move.Whip => Feel.WhipReach, _ => Feel.PunchReach,
    };
    float Push => move switch { Move.Kick => Feel.KickPush, Move.Whip => Feel.WhipPush, _ => Feel.JabPush };
    int Damage => move switch { Move.Punch or Move.Whip => 1, Move.SpinKick => 3, _ => 2 };
    bool Launches => move is Move.Smash or Move.JumpKick or Move.SpinKick;
    bool WindingUp => state == State.Attack && stateFrames < Startup;

    public bool CanAct => state == State.Idle || state == State.Walk || state == State.Run;
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
        boot = MakeSprite("BigBoot", Art.BigBoot);
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return sr;
    }

    // ground attacks; knee and throw only while holding someone
    public void Attack(Move m)
    {
        bool holding = state == State.Grab && (m == Move.Knee || m == Move.Throw);
        if (!CanAct && !holding) return;
        move = m;
        punchAlt = !punchAlt;
        if (m != Move.SpinKick) vel = Vector2.zero; // a spin kick keeps the run's momentum
        if (m == Move.KneeDrop) heightVel = Feel.KneeDropHop;
        Enter(State.Attack);
    }

    public bool IsDownedEnemyInReach(float reach)
    {
        foreach (var t in Game.Fighters)
        {
            float dx = (t.ground.x - ground.x) * facing;
            if (t.team != team && t.state == State.Down && dx > -4f && dx < reach
                && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand)
                return true;
        }
        return false;
    }

    public void StartRun(int dir)
    {
        if (!CanAct) return;
        facing = dir;
        Enter(State.Run);
    }

    public void Jump()
    {
        if (!CanAct) return;
        heightVel = Feel.JumpVel;
        airKick = airKickSpent = false;
        Enter(State.Jump);
    }

    public void JumpKick()
    {
        if (state != State.Jump || airKick) return;
        airKick = true;
        move = Move.JumpKick;
    }

    public void Grab(Fighter t)
    {
        grabbed = t;
        facing = t.ground.x > ground.x ? 1 : -1;
        vel = Vector2.zero;
        Enter(State.Grab);
        t.Enter(State.Held);
        t.vel = Vector2.zero;
        t.facing = -facing;
        HoldVictim();
    }

    public void TakeHit(int damage, int dir, bool launch, float push = Feel.JabPush)
    {
        if (!CanBeHit) return;
        bool countered = move == Move.Whip && WindingUp; // punished mid wind-up: knocked flying
        Release();
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

    // hammer stomp: driven into the ground up to the chest; a finishing blow knocks them flying instead
    public void Bury(int damage, int dir)
    {
        if (!CanBeHit) return;
        if (health <= damage) { TakeHit(damage, dir, true); return; }
        Release();
        health -= damage;
        flashFrames = 8;
        vel = Vector2.zero;
        Enter(State.Buried);
    }

    // the spinning leg reaches both sides
    bool InSpinReach(Fighter t) => t.CanBeHit && Mathf.Abs(t.ground.x - ground.x) < Reach
        && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand
        && Mathf.Abs(t.height - height) < Feel.JumpKickHeight;

    public bool InReach(Fighter t, float reach, float heightBand = Feel.HeightBand)
    {
        float dx = (t.ground.x - ground.x) * facing;
        return t.CanBeHit && dx > -4f && dx < reach
            && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand
            && Mathf.Abs(t.height - height) < heightBand;
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
            case State.Run:
                // keeps running while the direction is held; letting go or turning stops it
                vel = new Vector2(facing * Feel.RunX, moveInput.y * walkDepth);
                if (moveInput.x * facing <= 0f) Enter(State.Idle);
                break;
            case State.Jump:
                if (airKick && !airKickSpent) strikePending = true;
                if (landed) Enter(State.Idle);
                break;
            case State.Attack:
                if (grabbed != null) HoldVictim();
                if (stateFrames == Startup) strikePending = true;
                if (move == Move.SpinKick && stateFrames >= Startup && stateFrames < Startup + Active)
                {
                    // spins forward just above the ground, leg out, hitting everyone on both sides
                    vel = new Vector2(facing * Feel.SpinKickSpeed, 0f);
                    height = Feel.SpinKickHeight;
                    heightVel = 0f;
                    strikePending = true;
                }
                if (stateFrames >= Startup + Active + Recovery)
                    Enter(grabbed != null && grabbed.state == State.Held ? State.Grab : State.Idle);
                break;
            case State.Grab:
                if (grabbed == null || grabbed.state != State.Held || stateFrames >= Feel.GrabFrames) Release();
                else HoldVictim();
                break;
            case State.Held:
                vel = Vector2.zero; // the holder positions us
                break;
            case State.Buried:
                vel = Vector2.zero;
                if (stateFrames >= Feel.BuriedFrames) Enter(State.GetUp); // pops out
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

    void HoldVictim() => grabbed.ground = new Vector2(ground.x + facing * Feel.BodyGap, ground.y);

    // lets go of the held fighter (if any); they stagger free
    void Release()
    {
        if (grabbed != null && grabbed.state == State.Held) grabbed.Enter(State.Hurt);
        grabbed = null;
        if (state == State.Grab) Enter(State.Idle);
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
        if (move == Move.Knee || move == Move.Throw) StrikeHeld();
        else Strike();
    }

    void Strike()
    {
        if (move == Move.KneeDrop) { StrikeDowned(); return; }
        bool hit = false;
        bool spin = move == Move.SpinKick;
        float heightBand = move is Move.JumpKick or Move.SpinKick ? Feel.JumpKickHeight : Feel.HeightBand;
        foreach (var t in Game.Fighters)
            if (t.team != team && t.state != State.Held && (spin ? InSpinReach(t) : InReach(t, Reach, heightBand)))
            {
                int dir = spin ? (t.ground.x >= ground.x ? 1 : -1) : facing; // a spin knocks them away from us
                if (move == Move.Pound) t.Bury(Damage, dir);
                else t.TakeHit(Damage, dir, Launches, Push);
                hit = true;
            }
        if (!hit) return;
        if (move == Move.JumpKick) airKickSpent = true;
        bool big = Launches || move == Move.Pound;
        Game.Freeze(big ? Feel.SmashHitstop : Feel.Hitstop, big ? Feel.SmashShake : 0);
    }

    // knee drop: lands on enemies lying on the ground and keeps them down
    void StrikeDowned()
    {
        bool hit = false;
        foreach (var t in Game.Fighters)
        {
            float dx = (t.ground.x - ground.x) * facing;
            if (t.team == team || t.state != State.Down || dx <= -4f || dx >= Reach
                || Mathf.Abs(t.ground.y - ground.y) >= Feel.DepthBand) continue;
            t.health -= 2;
            t.flashFrames = 8;
            t.Enter(State.Down);
            hit = true;
        }
        if (hit) Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
    }

    // knee: damage without letting go (the last point knocks them flying); throw: over the shoulder
    void StrikeHeld()
    {
        if (grabbed == null || grabbed.state != State.Held) return;
        var g = grabbed;
        if (move == Move.Knee && g.health > 1)
        {
            g.health--;
            g.flashFrames = 8;
            Game.Freeze(Feel.Hitstop, 0);
            return;
        }
        grabbed = null;
        g.Enter(State.Hurt); // makes them hittable again
        bool thrown = move == Move.Throw;
        g.TakeHit(thrown ? 2 : 1, thrown ? -facing : facing, true);
        Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
    }

    // a body flying fast knocks down its own teammates
    void HitBodiesInPath()
    {
        if (Mathf.Abs(vel.x) < Feel.BodyHitSpeed) return;
        int dir = vel.x > 0f ? 1 : -1;
        foreach (var o in Game.Fighters)
            if (o != this && o.team == team && o.CanBeHit && o.state != State.Held
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
            State.Run => art.Run[Game.Frame / 5 % 4],
            State.Jump => airKick ? art.Kick : art.Crouch,
            State.GetUp => art.GetUp,
            State.Grab => art.Punch1,
            State.Held => art.Hurt,
            State.Buried => art.Sinking[Mathf.Min(stateFrames, art.Sinking.Length - 1)], // hammered in over a few frames
            State.Attack => move switch
            {
                Move.Whip => WindingUp ? art.WindUp : striking ? art.Punch2 : art.Idle[0],
                Move.Smash or Move.Kick => striking ? art.Kick : art.Crouch,
                Move.SpinKick => striking ? art.Kick : art.Run[0],
                Move.Pound => striking ? art.Kick : art.Crouch,
                Move.KneeDrop => art.Crouch,
                Move.Knee => striking ? art.Headbutt : art.Punch1,
                Move.Throw => art.Throw[WindingUp ? 0 : 1],
                _ => striking ? (punchAlt ? art.Punch2 : art.Punch1) : art.Idle[0],
            },
            State.Hurt => art.Hurt,
            _ => art.Fall, // launched, down, dead
        };
        // spinning: the body turns to face the other way every few frames
        bool spinning = state == State.Attack && move == Move.SpinKick && striking;
        int look = spinning && stateFrames / Feel.SpinTurnFrames % 2 == 1 ? -facing : facing;
        body.flipX = look < 0;
        body.transform.localPosition = new Vector3(0f, h, 0f);
        shadow.enabled = state != State.Buried;
        body.enabled = invulnFrames == 0 || invulnFrames / 3 % 2 == 0;
        bool telegraph = move == Move.Whip && WindingUp && stateFrames / 3 % 2 == 0;
        body.color = flashFrames > 0 && flashFrames / 2 % 2 == 0 ? new Color(1f, 0.45f, 0.45f)
            : telegraph ? new Color(1f, 1f, 0.45f)
            : Alive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

        // finishing kicks: the foot swells into a giant boot at the moment of impact
        boot.enabled = striking && state == State.Attack && (move == Move.Smash || move == Move.SpinKick);
        float swell = stateFrames - Startup < 2 ? 1.5f : 2f;
        boot.transform.localScale = new Vector3(look * swell, swell, 1f);
        boot.transform.localPosition = new Vector3(look * 10f, h + 15f, 0f);

        whipLine.enabled = move == Move.Whip && striking;
        whipLine.transform.localScale = new Vector3(facing, 1f, 1f);
        whipLine.transform.localPosition = new Vector3(facing * 10f, h + 19f, 0f);

        // nearer to the camera (lower depth) draws on top; shadows under everything
        int order = -Mathf.RoundToInt(ground.y) * 2;
        body.sortingOrder = order;
        whipLine.sortingOrder = order + 1;
        boot.sortingOrder = order + 1;
    }
}
