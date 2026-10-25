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
        var p = Game.Spawn(0, Color.green, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Color.green, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Color.red, new Vector2(55f, -70f), 10);

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
        var p = Game.Spawn(0, Color.green, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Color.red, new Vector2(55f, -70f + Feel.DepthBand + 1f), 10);
        Assert.IsFalse(p.InReach(e, Feel.PunchReach));

        e.ground.y = -70f;
        Assert.IsTrue(p.InReach(e, Feel.PunchReach));
    }

    [Test]
    public void HitstopFreezesEveryone()
    {
        var p = Game.Spawn(0, Color.green, new Vector2(40f, -70f), 10);
        p.Jump();
        Game.Simulate();
        float h = p.height;

        Game.Freeze(5, 0);
        Run(5);
        Assert.AreEqual(h, p.height);

        Run(1);
        Assert.Greater(p.height, h);
    }
}
