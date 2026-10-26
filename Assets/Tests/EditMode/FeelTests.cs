using NUnit.Framework;
using UnityEngine;

public class FeelTests
{
    [SetUp] public void SetUp() => Game.Reset();

    [TearDown]
    public void TearDown()
    {
        foreach (var f in Game.Fighters) Object.DestroyImmediate(f.gameObject);
        Game.Reset();
    }

    static void Run(int frames)
    {
        for (int i = 0; i < frames; i++) Game.Simulate();
    }

    [Test]
    public void JumpLandsAfterArc()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        p.Jump();
        int frames = 0;
        while (p.state == State.Jump && frames < 120) { Game.Simulate(); frames++; }

        int expected = Mathf.RoundToInt(2f * Feel.JumpVel / Feel.Gravity);
        Assert.That(frames, Is.InRange(expected - 2, expected + 2));
        Assert.AreEqual(0f, p.height);
    }

    [Test]
    public void JabHurtsSmashLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);

        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, e.state);

        Run(40);
        p.Attack(Move.Smash);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Launched, e.state);

        Run(Feel.SmashHitstop + 30);
        Assert.Greater(e.ground.x, 100f);
    }

    [Test]
    public void NoHitOutsideDepthBand()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f + Feel.DepthBand + 1f), 10);
        Assert.IsFalse(p.InReach(e, Feel.PunchReach));

        e.ground.y = -70f;
        Assert.IsTrue(p.InReach(e, Feel.PunchReach));
    }

    [Test]
    public void HitstopFreezesEveryone()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(200f, -70f), 10);
        p.Jump();
        e.Jump();
        Game.Simulate();
        float hp = p.height, he = e.height;

        Game.Freeze(5, 0);
        Run(5);
        Assert.AreEqual(hp, p.height);
        Assert.AreEqual(he, e.height);

        Run(1);
        Assert.Greater(p.height, hp);
        Assert.Greater(e.height, he);
    }

    [Test]
    public void SamePunchFrameTrades()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        e.facing = -1;
        p.Attack(Move.Punch);
        e.Attack(Move.Punch);

        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, p.state);
        Assert.AreEqual(State.Hurt, e.state);
    }

    static Fighter Lasher(float x)
    {
        var w = Game.Spawn(1, Art.Lasher, new Vector2(x, -70f), 10);
        w.whip = true;
        w.facing = -1;
        return w;
    }

    [Test]
    public void WhipHitsFromBeyondPunchRange()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var w = Lasher(40f + 45f);
        Assert.Greater(45f, Feel.PunchReach);

        w.Attack(Move.Whip);
        Run(Feel.WhipStartup);
        Assert.AreEqual(State.Hurt, p.state);
    }

    [Test]
    public void PunchDuringWhipWindupLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var w = Lasher(55f);
        w.Attack(Move.Whip);
        Run(2);

        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Launched, w.state);
    }

    [Test]
    public void KickReachesFurtherThanPunch()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(40f + 25f, -70f), 10);
        Assert.Greater(25f, Feel.PunchReach);

        p.Attack(Move.Kick);
        Run(Feel.KickStartup);
        Assert.AreEqual(State.Hurt, e.state);
        Assert.AreEqual(8, e.health);
    }

    [Test]
    public void JumpKickLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(60f, -70f), 10);
        p.Jump();
        Run(3);
        p.JumpKick();
        Run(2);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void GrabHeadbuttsThenThrowsBehind()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(52f, -70f), 10);
        p.Grab(e);
        Assert.AreEqual(State.Held, e.state);

        p.Attack(Move.Knee);
        Run(Feel.KneeStartup + 30);
        Assert.AreEqual(State.Held, e.state, "a headbutt keeps the hold");
        Assert.AreEqual(9, e.health);

        p.Attack(Move.Throw);
        Run(Feel.ThrowStartup);
        Assert.AreEqual(State.Launched, e.state);
        Assert.Less(e.vel.x, 0f, "thrown over the shoulder, behind the player");
    }

    [Test]
    public void RunIsFasterThanWalkAndDashKickLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        p.moveInput = Vector2.right;
        p.StartRun(1);
        Run(10);
        Assert.AreEqual(State.Run, p.state);
        Assert.AreEqual(Feel.RunX, p.vel.x);
        Assert.Greater(Feel.RunX, Feel.WalkX);

        var e = Game.Spawn(1, Art.GruntA, new Vector2(p.ground.x + 20f, -70f), 10);
        p.Attack(Move.DashKick);
        Run(Feel.DashKickStartup);
        Assert.AreEqual(State.Launched, e.state);
        Assert.AreEqual(7, e.health);
    }

    [Test]
    public void PoundBuriesThenEnemyPopsOut()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        p.Attack(Move.Pound);
        Run(Feel.PoundStartup);
        Assert.AreEqual(State.Buried, e.state);

        Run(Feel.BuriedFrames + Feel.SmashHitstop);
        Assert.AreEqual(State.GetUp, e.state);
    }

    [Test]
    public void KneeDropHitsEnemyLyingDown()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        e.TakeHit(1, 1, true);
        while (e.state != State.Down) Game.Simulate();
        e.ground.x = 55f; // the launch carried them away; put them back in front, no longer sliding
        e.vel = Vector2.zero;
        Assert.IsTrue(p.IsDownedEnemyInReach(Feel.KneeDropReach));

        p.Attack(Move.KneeDrop);
        Run(Feel.KneeDropStartup);
        Assert.AreEqual(7, e.health);
        Assert.AreEqual(State.Down, e.state);
    }
}
