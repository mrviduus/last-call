using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SmokeTest
{
    // Unattended run in the bar: furniture and seated couples in place; once the fight starts the patrons get up
    // and hit the idle hero, with no errors logged.
    [UnityTest]
    public IEnumerator BarFurnitureAndEnemiesReachTheHero()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(36, Game.Props.Count, "stools, tables, chairs, bottles, pool tables with balls and cues");
        Assert.AreEqual(8, Game.Fighters.FindAll(f => f.state == State.Sit).Count, "a couple at each round table and one at the counter");

        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        Assert.AreEqual(8, Game.Fighters.FindAll(f => f.state == State.Sit).Count, "nobody gets up before the fight starts");

        Game.Brawl = true; // as if the hero threw the first punch
        for (int i = 0; i < 1500; i++) yield return new WaitForFixedUpdate();
        Assert.IsFalse(Game.Fighters.Exists(f => f.state == State.Sit), "the whole bar joined the fight");
        Assert.Less(Game.Hero.health, Game.Hero.maxHealth, "enemies reached and hit the hero");

        Shot("smoke");
    }

    // waves for a minute from the first punch, the boss at midnight, then the win card; every enemy is knocked out as it arrives
    [UnityTest]
    public IEnumerator LastCallEndsWithTheBossAndAWin()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        Game.RestartRequested = true;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Game.Hero.maxHealth = Game.Hero.health = 100000;
        Game.Brawl = true;
        bool sawBoss = false;
        for (int i = 0; i < 60 * 120 && !Game.Won; i++)
        {
            foreach (var e in Game.Fighters)
                if (e.team != 0 && e.Alive) { sawBoss |= e.armored; e.TakeHit(9999, 1, true); }
            yield return new WaitForFixedUpdate();
        }
        Assert.IsTrue(sawBoss, "the closer came at midnight");
        Assert.IsTrue(Game.Won);
        Assert.Greater(Game.Score, 0);
        Shot("win");
    }

    // the hero's killing blows on enemies in front of him, one after another; screenshots to look at the gore
    [UnityTest]
    public IEnumerator GoreFinishers()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        Game.RestartRequested = true; // a fresh bar, whatever an earlier test left
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var hero = Game.Hero;
        hero.ground = new Vector2(500f, -50f);
        hero.facing = 1;

        var e = Victim(hero, 32f, 1);
        hero.Attack(Move.Kick); // a killing kick: off with the head
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(e.headless);
        Shot("gore_head");

        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        e = Victim(hero, 30f, 100);
        e.Bury(10, 1);
        hero.Attack(Move.Kick); // a kick in the face of someone buried: punted
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(e.headless);
        Shot("gore_punt");

        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        e = Victim(hero, 26f, 100);
        hero.Grab(e);
        hero.Attack(Move.Rip); // an arm torn off
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(e.armless);
        Assert.IsNotNull(hero.held);
        Shot("gore_arm");
        LogAssert.NoUnexpectedReceived();
    }

    // the dirty moves in a hold, mid-move: screenshots to look at the poses
    [UnityTest]
    public IEnumerator DirtyMoves()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        Game.RestartRequested = true;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var hero = Game.Hero;
        hero.ground = new Vector2(500f, -50f);
        hero.facing = 1;

        var guy = Game.Spawn(1, Art.Guy, hero.ground + new Vector2(24f, 0f), 100);
        hero.Grab(guy);
        hero.Attack(Move.LowBlow);
        for (int i = 0; i < Feel.KneeStartup + 2; i++) yield return new WaitForFixedUpdate();
        Shot("lowblow_knee");
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        Assert.AreEqual(State.Hurt, guy.state);
        Shot("lowblow_doubled");

        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        hero.ground = new Vector2(700f, -50f);
        var girl = Victim(hero, 24f, 100);
        hero.Grab(girl);
        hero.Attack(Move.HairSlam);
        for (int i = 0; i < Feel.HairSlamStartup - 2; i++) yield return new WaitForFixedUpdate();
        Assert.Greater(girl.lift, 10f, "lifted by the hair");
        Shot("hairslam_lift");
        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        Shot("hairslam_fall");
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.AreEqual(State.Down, girl.state);
        Shot("hairslam_down");
    }

    static Fighter Victim(Fighter hero, float ahead, int hp)
    {
        var e = Game.Spawn(1, Art.GruntA, hero.ground + new Vector2(ahead, 0f), hp);
        e.facing = -1;
        return e;
    }

    static void Shot(string name)
    {
        Camera.main.Render(); // batchmode has no end-of-frame, so draw the pixel screen by hand
        var tex = new Texture2D(Game.ScreenW, Game.ScreenH);
        RenderTexture.active = Game.PixelScreen;
        tex.ReadPixels(new Rect(0, 0, Game.ScreenW, Game.ScreenH), 0, 0);
        RenderTexture.active = null;
        Directory.CreateDirectory("Logs");
        File.WriteAllBytes("Logs/" + name + ".png", tex.EncodeToPNG());
        Object.Destroy(tex);
    }
}
