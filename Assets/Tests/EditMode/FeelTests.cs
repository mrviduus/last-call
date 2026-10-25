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

        p.Attack(false);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, e.state);

        Run(40);
        p.Attack(true);
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
        p.Attack(false);
        e.Attack(false);

        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, p.state);
        Assert.AreEqual(State.Hurt, e.state);
    }
}
