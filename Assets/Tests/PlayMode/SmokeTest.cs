using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SmokeTest
{
    // Unattended run: enemies must spawn, walk in and hit the idle hero, with no errors logged.
    [UnityTest]
    public IEnumerator EnemiesReachAndHitHero()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        for (int i = 0; i < 900; i++) yield return new WaitForFixedUpdate();

        Assert.IsTrue(Game.Fighters.Exists(f => f.team != 0), "enemies spawned");
        Assert.Less(Game.Hero.health, Game.Hero.maxHealth, "enemies hit the hero");

        Camera.main.Render(); // batchmode has no end-of-frame, so draw the pixel screen by hand
        var tex = new Texture2D(Game.ScreenW, Game.ScreenH);
        RenderTexture.active = Game.PixelScreen;
        tex.ReadPixels(new Rect(0, 0, Game.ScreenW, Game.ScreenH), 0, 0);
        RenderTexture.active = null;
        File.WriteAllBytes("Logs/smoke.png", tex.EncodeToPNG());
    }
}
