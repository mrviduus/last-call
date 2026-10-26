using UnityEngine;

public enum State { Idle, Walk, Run, Jump, Attack, Hurt, Launched, Down, GetUp, Dead, Grab, Held, Buried, Sit, Hang }
public enum Move { Punch, Smash, Kick, JumpKick, FlyingKick, BicycleKick, Pound, ElbowDrop, Knee, Throw, Whip, Swing, ThrowProp, HangKick, Special }

// 2.5D belt-scroller body. Logic runs in Step() at a fixed 60 Hz, in pixels and frames.
// Ground plane = (x, depth); height = jump height. Screen y = depth + height.
public class Fighter : MonoBehaviour
{
    const float Width = 28f; // body width for bodies hitting bodies

    public int team;
    public string look; // for the HUD name
    public int maxHealth = 5;
    public float walkX = Feel.WalkX, walkDepth = Feel.WalkDepth;
    public System.Action Think; // brain (Player or Enemy), runs before each tick

    public State state;
    public int stateFrames;
    public Vector2 ground, vel, moveInput;
    public float height, heightVel;
    public int health, facing = 1;
    public int recoverable; // health spent on the special, won back by landing hits, lost when hit (Streets of Rage 4)
    public Move move;
    public bool whip; // enemy type: long reach, slow telegraphed wind-up
    public bool faceMove = true; // walking turns us; enemies turn to the hero themselves
    public Fighter grabbed;
    public Prop held; // chair or bottle in hand
    public Lamp hangingOn;
    int hangKick; // frames left of a kick from the lamp

    int flashFrames, invulnFrames;
    public int comboHits;        // hits taken in the current combo (since we last stood free)
    public bool lastStrikeHit;   // our last blow connected: the punch combo goes on only on hits
    bool bounced, strikePending, punchAlt, airKick;
    SpriteRenderer body, shadow, whipLine;
    Material normalMat;
    int landSquash, jumpStretch; // frames left of squashing on landing / stretching on take-off
    Art.Frames art;

    int Startup => move switch
    {
        Move.Kick => Feel.KickStartup, Move.FlyingKick => Feel.FlyingKickStartup, Move.BicycleKick => Feel.BicycleKickStartup,
        Move.Pound => Feel.PoundStartup, Move.ElbowDrop => Feel.ElbowDropStartup, Move.Knee => Feel.KneeStartup,
        Move.Throw => Feel.ThrowStartup, Move.Whip => Feel.WhipStartup,
        Move.Swing => Feel.SwingStartup, Move.ThrowProp => Feel.PropThrowStartup, Move.Special => Feel.SpecialStartup,
        _ => team == 0 ? Feel.PunchStartup : Feel.EnemyPunchStartup,
    };
    int Active => move switch
    {
        Move.Kick => Feel.KickActive, Move.FlyingKick => Feel.FlyingKickActive, Move.BicycleKick => Feel.BicycleKickActive,
        Move.Swing => Feel.SwingActive, Move.Whip => Feel.WhipActive, Move.Special => Feel.SpecialActive, _ => Feel.PunchActive,
    };
    int Recovery => move switch
    {
        Move.Smash => Feel.SmashRecovery, Move.Kick => Feel.KickRecovery,
        Move.FlyingKick => Feel.FlyingKickRecovery, Move.BicycleKick => Feel.BicycleKickRecovery, Move.Pound => Feel.PoundRecovery,
        Move.ElbowDrop => Feel.ElbowDropRecovery, Move.Knee => Feel.KneeRecovery,
        Move.Throw => Feel.ThrowRecovery, Move.Whip => Feel.WhipRecovery,
        Move.Swing => Feel.SwingRecovery, Move.ThrowProp => Feel.PropThrowRecovery, Move.Special => Feel.SpecialRecovery,
        _ => Feel.PunchRecovery,
    };
    float Reach => move switch
    {
        Move.Smash => Feel.SmashReach, Move.Kick => Feel.KickReach, Move.Special => Feel.SpecialReach,
        Move.JumpKick => Feel.JumpKickReach, Move.FlyingKick => Feel.FlyingKickReach, Move.BicycleKick => Feel.BicycleKickReach, Move.Pound => Feel.PoundReach,
        Move.ElbowDrop => Feel.ElbowDropReach, Move.HangKick => Feel.HangKickReach,
        Move.Whip => Feel.WhipReach, Move.Swing when held != null => held.Reach, _ => Feel.PunchReach,
    };
    float Push => move switch
    {
        Move.Kick => Feel.KickPush, Move.Whip => Feel.WhipPush,
        Move.BicycleKick => Feel.BicycleKickSpeed * 1.6f, // carries them along in front of the kicks
        _ => Feel.JabPush,
    };
    int Damage => move switch
    {
        Move.Smash => Feel.SmashDamage, Move.Kick => Feel.KickDamage, Move.JumpKick or Move.HangKick => Feel.JumpKickDamage,
        Move.FlyingKick => Feel.FlyingKickDamage, Move.BicycleKick => Feel.BicycleKickDamage, Move.Pound => Feel.PoundDamage,
        Move.ElbowDrop => Feel.ElbowDropDamage, Move.Whip => Feel.WhipDamage, Move.Special => Feel.SpecialDamage, Move.Swing when held != null => held.Damage,
        _ => team == 0 ? Feel.PunchDamage : Feel.EnemyPunchDamage,
    };
    bool Launches => move is Move.Smash or Move.JumpKick or Move.FlyingKick or Move.HangKick or Move.Special
        || move == Move.BicycleKick && stateFrames >= Startup + Active - Feel.BicycleHitGap // only its last kick
        || move == Move.Swing && held != null && held.kind is Prop.Kind.Chair or Prop.Kind.Lamp;
    bool WindingUp => state == State.Attack && stateFrames < Startup;

    public bool CanAct => state == State.Idle || state == State.Walk || state == State.Run;
    public bool Alive => health > 0;
    public bool Solid => Alive && height <= 0f && state != State.Launched && state != State.Down && state != State.Sit && state != State.Hang;
    public bool CanBeHit => Alive && invulnFrames == 0
        && state != State.Launched && state != State.Down && state != State.GetUp;

    public void Init(int team, string look, Vector2 start)
    {
        this.team = team;
        this.look = look;
        ground = start;
        health = maxHealth;
        art = Art.For(look);
        shadow = MakeSprite("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000; // under every body
        body = MakeSprite("Body", art.Idle[0]);
        normalMat = body.sharedMaterial;
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

    // ground attacks; knee and throw only while holding someone
    public void Attack(Move m)
    {
        bool holding = state == State.Grab && (m == Move.Knee || m == Move.Throw);
        if (!CanAct && !holding) return;
        move = m;
        punchAlt = !punchAlt;
        if (m != Move.BicycleKick && m != Move.FlyingKick) vel = Vector2.zero; // running kicks keep the momentum
        if (m == Move.ElbowDrop) heightVel = Feel.ElbowDropHop;
        Enter(State.Attack);
        Sfx.Play("whoosh", team == 0 ? 0.5f : 0.3f);
    }

    // desperation move (J+K): a spinning kick all round that can't be interrupted, even from hitstun;
    // costs health, which comes back by landing hits before getting hit again
    public bool Special()
    {
        bool from = CanAct || state == State.Hurt || state == State.Jump || state == State.Attack && move == Move.Punch;
        if (!from || health <= Feel.SpecialCost) return false;
        if (held != null) { held.Drop(); held = null; }
        health -= Feel.SpecialCost;
        recoverable += Feel.SpecialCost;
        move = Move.Special;
        vel = Vector2.zero;
        height = heightVel = 0f; // on the ground: J+K a moment apart may have started a jump first
        airKick = false;
        Enter(State.Attack);
        invulnFrames = Feel.SpecialStartup + Feel.SpecialActive;
        Sfx.Play("whoosh", 0.8f);
        return true;
    }

    public bool IsDownedEnemyInReach(float reach)
    {
        foreach (var t in Game.Fighters)
        {
            float dx = (t.ground.x - ground.x) * facing;
            if (t.team != team && t.Alive && t.state == State.Down && dx > -4f && dx < reach
                && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand)
                return true;
        }
        return false;
    }

    public void StartRun(int dir)
    {
        if (!CanAct || held != null) return; // no running with a chair in your hands
        facing = dir;
        Enter(State.Run);
    }

    // a patron sitting at a table (or up on a bar stool: seatHeight), facing it, until the bar fight starts
    public void Sit(int faceDir, float seatHeight = 0f)
    {
        facing = faceDir;
        vel = Vector2.zero;
        height = seatHeight;
        heightVel = 0f;
        Enter(State.Sit);
    }

    public void StandUp()
    {
        if (state == State.Sit) Enter(State.Idle);
    }

    // jumped into a lamp: hang from it by the hands
    public void Hang(Lamp lamp)
    {
        hangingOn = lamp;
        lamp.hanger = this;
        vel = Vector2.zero;
        heightVel = 0f;
        ground.x = lamp.SwingX; // hands on the rim right away
        height = lamp.bottom - Feel.HangReach;
        hangKick = 0;
        Enter(State.Hang);
    }

    public void HangKick()
    {
        if (state != State.Hang || hangKick > 0) return;
        hangKick = Feel.HangKickFrames;
        Sfx.Play("whoosh", 0.5f);
    }

    // let go: flung off in the direction of the swing
    public void LetGo()
    {
        if (hangingOn == null) return;
        vel = new Vector2(Mathf.Clamp(hangingOn.angVel * Lamp.Length, -5f, 5f), 0f);
        hangingOn.hanger = null;
        hangingOn = null;
        heightVel = 1f;
        airKick = false;
        Enter(State.Jump);
    }

    // pull the lamp off its cord and drop down holding it
    public void RipLamp()
    {
        var lamp = hangingOn;
        if (lamp == null) return;
        LetGo();
        vel = Vector2.zero;
        lamp.Rip();
        Sfx.Play("glass", 0.5f);
        var shade = Game.SpawnProp(Prop.Kind.Lamp, ground);
        held = shade;
        shade.holder = this;
    }

    public void Jump()
    {
        if (!CanAct) return;
        heightVel = Feel.JumpVel;
        airKick = false;
        jumpStretch = Feel.SquashFrames;
        Enter(State.Jump);
    }

    public void JumpKick()
    {
        if (state != State.Jump || airKick) return;
        airKick = true;
        move = Move.JumpKick;
    }

    public void PickUp(Prop p)
    {
        if (!CanAct || held != null || !p.Pickable) return;
        held = p;
        p.holder = this;
        p.resting = false;
    }

    // a free prop on the floor right in front or underneath
    public Prop PropInReach()
    {
        foreach (var p in Game.Props)
            if (p.Pickable && Mathf.Abs(p.ground.x - ground.x) < Feel.PickupRange
                && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
                return p;
        return null;
    }

    public bool EnemyInReach(float reach)
    {
        foreach (var t in Game.Fighters)
            if (t.team != team && InReach(t, reach)) return true;
        return false;
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
        strikePending = false; // hit first: our own blow never lands
        recoverable = 0;
        bool countered = move == Move.Whip && WindingUp; // punished mid wind-up: knocked flying
        Release();
        if (hangingOn != null) { hangingOn.hanger = null; hangingOn = null; } // knocked off the lamp
        if (held != null) { held.Drop(); held = null; }
        Damaged(damage);
        facing = -dir;
        // a long enough combo knocks them down whatever the last hit was: no endless juggles
        if (launch || countered || !Alive || height > 0f || state == State.Buried || comboHits >= Feel.JuggleCap)
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
            if (team == 0) invulnFrames = Feel.HeroHurtInvuln;
        }
    }

    // hammer stomp: driven into the ground up to the chest; a finishing blow knocks them flying instead
    public void Bury(int damage, int dir)
    {
        if (!CanBeHit) return;
        if (health <= damage || state == State.Buried) { TakeHit(damage, dir, true); return; } // once per bury
        Release();
        Damaged(damage);
        vel = Vector2.zero;
        Enter(State.Buried);
    }

    // every blow lands through here: from the ComboScaleFrom-th hit of a combo on, each does 10% less (down to half)
    void Damaged(int damage)
    {
        comboHits++;
        float scale = Mathf.Clamp(1f - 0.1f * (comboHits - Feel.ComboScaleFrom + 1), Feel.ComboScaleMin, 1f);
        int dealt = Mathf.Max(1, Mathf.RoundToInt(damage * scale));
        health -= dealt;
        flashFrames = 8;
        Fx.Spark(new Vector2(ground.x, ground.y + height + 34f), dealt >= Feel.SmashDamage);
        if (team != 0) Game.LastHit = this; // its health bar shows in the HUD
    }

    public bool InReach(Fighter t, float reach, float heightBand = Feel.HeightBand, bool allRound = false)
    {
        float dx = (t.ground.x - ground.x) * facing;
        if (allRound) dx = Mathf.Abs(dx);
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
        if (landSquash > 0) landSquash--;
        if (jumpStretch > 0) jumpStretch--;
        if (invulnFrames > 0) invulnFrames--;

        bool landed = ApplyPhysics();
        switch (state)
        {
            case State.Idle:
            case State.Walk:
                comboHits = 0;
                float carry = held != null ? Feel.CarrySpeed : 1f; // props are heavy
                vel = new Vector2(moveInput.x * walkX, moveInput.y * walkDepth) * carry;
                if (faceMove && moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                state = vel == Vector2.zero ? State.Idle : State.Walk;
                break;
            case State.Run:
                // keeps running while the direction is held; letting go or turning stops it
                vel = new Vector2(facing * Feel.RunX, moveInput.y * walkDepth);
                if (moveInput.x * facing <= 0f) Enter(State.Idle);
                if (stateFrames % 10 == 0) Fx.Dust(ground - new Vector2(facing * 8f, 0f)); // kicked up at the heels
                break;
            case State.Jump:
                if (airKick) strikePending = true; // hits everyone it passes through
                if (team == 0 && heightVel > 0f && !airKick && LampToGrab() is Lamp lamp) Hang(lamp);
                if (landed) { Enter(State.Idle); landSquash = Feel.SquashFrames; Fx.Dust(ground); }
                break;
            case State.Attack:
                if (grabbed != null) HoldVictim();
                if (stateFrames == Startup) strikePending = true;
                if (move == Move.FlyingKick && stateFrames >= Startup && stateFrames < Startup + Active)
                {
                    // flies straight across the screen, feet first, just above the ground
                    vel = new Vector2(facing * Feel.FlyingKickSpeed, 0f);
                    height = Feel.FlyingKickHeight;
                    heightVel = 0f;
                    strikePending = true;
                    if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body); // a trail behind the kick
                }
                if (move == Move.BicycleKick && stateFrames >= Startup && stateFrames < Startup + Active)
                {
                    // flies forward low, kicking left-right-left: a separate hit every BicycleHitGap frames
                    vel = new Vector2(facing * Feel.BicycleKickSpeed, 0f);
                    height = Feel.BicycleKickHeight;
                    heightVel = 0f;
                    if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body);
                    if ((stateFrames - Startup) % Feel.BicycleHitGap == 0) strikePending = true;
                }
                if (move == Move.Special && stateFrames >= Startup && stateFrames < Startup + Active)
                {
                    strikePending = true; // hits everyone around, each once (they fly off untouchable)
                    if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body);
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
            case State.Sit:
                vel = Vector2.zero;
                break;
            case State.Hang:
                // swinging from the lamp: pump with left/right, kick with J/L
                hangingOn.Pump(moveInput.x);
                ground.x = hangingOn.SwingX;
                height = hangingOn.bottom - Feel.HangReach;
                if (moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                if (hangKick > 0 && --hangKick == Feel.HangKickFrames - Feel.HangKickStartup)
                {
                    move = Move.HangKick;
                    strikePending = true;
                }
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
                if (landed && bounced) { Enter(State.Down); Sfx.Play("thud", 0.8f); Fx.Dust(ground); }
                else if (landed) { bounced = true; heightVel = Feel.BounceUp; vel.x *= 0.5f; Fx.Dust(ground); }
                break;
            case State.Down:
                if (stateFrames >= Feel.DownFrames) Enter(Alive ? State.GetUp : State.Dead);
                break;
            case State.GetUp:
                if (stateFrames >= Feel.GetUpFrames) { Enter(State.Idle); invulnFrames = Feel.GetUpInvuln; }
                break;
        }
    }

    Lamp LampToGrab()
    {
        foreach (var l in Game.Lamps)
            if (l.hanger == null && Mathf.Abs(l.SwingX - ground.x) < Feel.LampGrabX
                && Mathf.Abs(l.depth - ground.y) < Feel.LampGrabDepth
                && height >= l.bottom - Feel.HangReach - 6f)
                return l;
        return null;
    }

    void HoldVictim() => grabbed.ground = new Vector2(ground.x + facing * Feel.BodyGap, ground.y);

    // lets go of the held fighter (if any); they stagger free
    void Release()
    {
        if (grabbed != null && grabbed.state == State.Held) { grabbed.Enter(State.Hurt); grabbed.invulnFrames = Feel.BreakFreeInvuln; }
        grabbed = null;
        if (state == State.Grab) Enter(State.Idle);
    }

    // returns true on the frame the body touches the ground
    bool ApplyPhysics()
    {
        if (state == State.Sit || state == State.Hang) return false; // held up by a seat, or hanging from a lamp
        bool grounded = height <= 0f && heightVel <= 0f;
        if (grounded && !CanAct) vel.x = Mathf.MoveTowards(vel.x, 0f, Feel.Friction);
        ground += vel;
        ground.x = Mathf.Clamp(ground.x, Game.MinX, Game.MaxX);
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        if (grounded) return false;

        heightVel -= Feel.Gravity;
        height += heightVel;
        if (height > 0f) return false;
        height = 0f;
        heightVel = 0f;
        return true;
    }

    // called by Game after every fighter has stepped; the hero's first, so the hero wins a same-frame tie
    public void ResolveStrike()
    {
        if (!strikePending) return;
        strikePending = false;
        if (move == Move.Knee || move == Move.Throw) StrikeHeld();
        else if (move == Move.ThrowProp) { held?.Throw(this); held = null; }
        else Strike();
    }

    void Strike()
    {
        if (move == Move.ElbowDrop) { StrikeDowned(); return; }
        bool hit = false, solidHit = false, finished = false;
        int landed = 0;
        bool allRound = move == Move.Special;
        float heightBand = move is Move.JumpKick or Move.FlyingKick or Move.BicycleKick or Move.HangKick ? Feel.JumpKickHeight : Feel.HeightBand;
        foreach (var t in Game.Fighters)
            if (t.team != team && t.state != State.Held && InReach(t, Reach, heightBand, allRound))
            {
                if (move == Move.Pound) t.Bury(Damage, facing); // a knockout launches instead
                else t.TakeHit(Damage, allRound ? (t.ground.x >= ground.x ? 1 : -1) : facing, Launches, Push);
                if (team == 0 && !t.Alive) { Finish(t); finished = true; }
                hit = solidHit = true;
                landed++;
            }
        // tables tip over and slide, bottles get swept off the counter (backwards: a bottle may break)
        for (int i = Game.Props.Count - 1; i >= 0; i--)
        {
            var p = Game.Props[i];
            float ahead = (p.ground.x - ground.x) * facing;
            if (p.holder == null && p.thrower == null && ahead > -4f && ahead < Reach
                && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
            {
                bool table = p.Heavy; // tables and pool tables are solid hits, loose bottles are not
                if (p.Struck(facing, this)) { hit = true; solidHit |= table; }
            }
        }
        lastStrikeHit = hit;
        if (landed > 0 && recoverable > 0 && move != Move.Special) // hits win back the special's cost
        {
            int back = Mathf.Min(recoverable, landed * Feel.RecoverPerHit);
            health += back;
            recoverable -= back;
        }
        if (solidHit && move == Move.Swing) held?.UseUp(); // wears out on people and tables, not on loose bottles
        if (hit) Sfx.Play(finished || Launches ? "hit_heavy" : "hit");
        if (!hit || finished) return; // a finisher already set its own freeze
        bool big = Launches || move == Move.Pound;
        Game.Freeze(big ? Feel.SmashHitstop : Feel.Hitstop, big ? Feel.SmashShake : 0);
    }

    // the player's last hit on an enemy: long freeze, flash, shake, sent flying far
    void Finish(Fighter t)
    {
        t.vel *= Feel.FinisherLaunch;
        t.heightVel *= Feel.FinisherLaunch;
        Game.Freeze(Feel.FinisherHitstop, Feel.FinisherShake);
        Game.Flash(Feel.FinisherFlash);
        if (!Game.Fighters.Exists(o => o.team != team && o.Alive && o.state != State.Sit)) Game.SlowMo(Feel.LastKillSlowMo); // the last one: slow motion
    }

    // wrestling elbow drop: lands elbow-first on enemies lying on the ground (doesn't keep them down longer);
    // when it knocks one out it is the finisher (long freeze, flash, shake)
    void StrikeDowned()
    {
        bool hit = false, finished = false;
        foreach (var t in Game.Fighters)
        {
            float dx = (t.ground.x - ground.x) * facing;
            if (t.team == team || !t.Alive || t.state != State.Down || dx <= -4f || dx >= Reach
                || Mathf.Abs(t.ground.y - ground.y) >= Feel.DepthBand) continue;
            t.Damaged(Damage);
            hit = true;
            finished |= !t.Alive;
        }
        if (finished)
        {
            Game.Freeze(Feel.FinisherHitstop, Feel.FinisherShake);
            Game.Flash(Feel.FinisherFlash);
            if (!Game.Fighters.Exists(o => o.team != team && o.Alive && o.state != State.Sit)) Game.SlowMo(Feel.LastKillSlowMo);
        }
        else if (hit) Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
        if (hit) Sfx.Play("hit_heavy");
    }

    // knee: damage without letting go (the last point knocks them flying); throw: over the shoulder
    void StrikeHeld()
    {
        if (grabbed == null || grabbed.state != State.Held) return;
        var g = grabbed;
        if (move == Move.Knee && g.health > Feel.KneeDamage)
        {
            g.Damaged(Feel.KneeDamage);
            Sfx.Play("hit");
            Game.Freeze(Feel.Hitstop, 0);
            return;
        }
        grabbed = null;
        g.Enter(State.Hurt); // makes them hittable again
        bool thrown = move == Move.Throw;
        g.TakeHit(thrown ? Feel.ThrowDamage : Feel.KneeDamage, thrown ? -facing : facing, true);
        Sfx.Play("hit_heavy");
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
                o.TakeHit(Feel.BodyHitDamage, dir, true);
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
            State.Idle => art.Idle[Game.Frame / 10 % art.Idle.Length],
            State.Walk => art.Walk[Game.Frame / 5 % art.Walk.Length],
            State.Run => art.Walk[Game.Frame / 3 % art.Walk.Length], // walk cycle, played fast
            State.Jump => airKick ? art.JumpKick : art.Air,
            State.GetUp => art.GetUp,
            State.Grab => art.Punch1,
            State.Held => art.Hurt,
            State.Sit => art.Sit,
            State.Hang => hangKick > 0 ? art.JumpKick : art.ElbowRise, // arms up on the lamp, or kicking
            State.Buried => art.Sinking[Mathf.Min(stateFrames, art.Sinking.Length - 1)], // hammered in over a few frames
            State.Attack => move switch
            {
                Move.Whip => WindingUp ? art.WindUp : striking ? art.Punch2 : art.Idle[0],
                Move.Smash or Move.Kick or Move.Pound => WindingUp ? art.KickWind : striking ? art.Kick : art.KickRecover,
                Move.FlyingKick => striking ? art.JumpKick : art.Crouch,
                Move.Swing => striking ? art.Punch2 : art.WindUp,
                Move.ThrowProp => striking ? art.Punch2 : art.WindUp,
                Move.BicycleKick => striking ? art.Bicycle[stateFrames / 3 % 2] : art.KickWind, // left-right-left
                Move.ElbowDrop => heightVel > 0f ? art.ElbowRise : art.ElbowDrop, // up with the elbow raised, down flat
                Move.Knee => striking ? art.Headbutt : art.Punch1,
                Move.Throw => art.Throw[WindingUp ? 0 : 1],
                Move.Special => striking ? art.Kick : art.KickWind,
                _ => striking ? (punchAlt ? art.Punch2 : art.Punch1) : WindingUp && team != 0 ? art.WindUp : art.Idle[0],
            },
            State.Hurt => art.Hurt,
            State.Launched => art.Fall,
            _ => art.Down, // down, dead
        };
        bool spinning = state == State.Attack && move == Move.Special && striking;
        body.flipX = spinning ? stateFrames / 4 % 2 == 0 : facing < 0; // the kick turns round and round
        // shakes in place through the hitstop; leans back winding up, lunges into the blow
        float jolt = Game.Frozen && flashFrames > 0 ? (Game.Frame % 2 == 0 ? Feel.HitJolt : -Feel.HitJolt) : 0f;
        float lunge = state != State.Attack || move is Move.FlyingKick or Move.BicycleKick or Move.ElbowDrop or Move.Throw or Move.Special ? 0f
            : WindingUp ? -Feel.WindUpLean : striking ? (move is Move.Punch or Move.Knee ? Feel.JabLunge : Feel.HeavyLunge) : 0f;
        body.transform.localPosition = new Vector3(jolt + lunge * facing, h, 0f);
        body.transform.localScale = landSquash > 0 ? new Vector3(1.12f, 0.88f, 1f)
            : jumpStretch > 0 ? new Vector3(0.9f, 1.1f, 1f) : Vector3.one;
        shadow.enabled = state != State.Buried;
        body.enabled = invulnFrames == 0 || spinning || invulnFrames / 3 % 2 == 0;
        bool telegraph = team != 0 && WindingUp && stateFrames / 3 % 2 == 0; // every enemy blow flashes as it winds up
        bool white = flashFrames > 5; // solid white the moment a blow lands, then red
        body.sharedMaterial = white ? Art.Silhouette : normalMat;
        body.color = white ? Color.white
            : flashFrames > 0 && flashFrames / 2 % 2 == 0 ? new Color(1f, 0.45f, 0.45f)
            : telegraph ? new Color(1f, 1f, 0.45f)
            : Alive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

        whipLine.enabled = move == Move.Whip && striking;
        whipLine.transform.localScale = new Vector3(facing * 1.8f, 1f, 1f);
        whipLine.transform.localPosition = new Vector3(facing * 20f, h + 38f, 0f);

        // nearer to the camera (lower depth) draws on top; shadows under everything
        int order = -Mathf.RoundToInt(ground.y) * 2;
        body.sortingOrder = state == State.Sit ? order + 1 : order; // in front of the chair
        whipLine.sortingOrder = order + 1;
    }
}
