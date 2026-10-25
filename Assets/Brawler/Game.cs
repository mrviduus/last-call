using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: 60 Hz fixed loop, 256x224 pixel screen, floor, player, enemy waves.
public class Game : MonoBehaviour
{
    public const int ScreenW = 256, ScreenH = 224;
    public const float MinX = 8f, MinDepth = -100f, MaxDepth = -40f;

    public static readonly List<Fighter> Fighters = new List<Fighter>();
    public static Fighter Hero;
    public static int Frame;    // every fixed tick
    public static int SimFrame; // ticks that actually simulated (hitstop excluded)
    public static bool RestartRequested;
    public static RenderTexture PixelScreen;
    static int freezeFrames, shakeFrames;

    Camera cam;
    float camX = ScreenW / 2f;
    int wave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    public static void Reset()
    {
        Fighters.Clear();
        Hero = null;
        Frame = SimFrame = freezeFrames = shakeFrames = 0;
        RestartRequested = false;
    }

    public static void Freeze(int frames, int shake)
    {
        freezeFrames = Mathf.Max(freezeFrames, frames);
        shakeFrames = Mathf.Max(shakeFrames, shake);
    }

    // one 60 Hz tick of the whole fight; hitstop skips it
    public static void Simulate()
    {
        Frame++;
        if (shakeFrames > 0) shakeFrames--;
        if (freezeFrames > 0) { freezeFrames--; return; }
        SimFrame++;
        for (int i = 0; i < Fighters.Count; i++) Fighters[i].Step();
        Separate();
        for (int i = 0; i < Fighters.Count; i++) Fighters[i].ResolveStrike();
    }

    // standing bodies on the same lane push apart, so nobody stands inside anyone else
    static void Separate()
    {
        for (int i = 0; i < Fighters.Count; i++)
            for (int j = i + 1; j < Fighters.Count; j++)
            {
                Fighter a = Fighters[i], b = Fighters[j];
                if (!a.Solid || !b.Solid || Mathf.Abs(a.ground.y - b.ground.y) >= Feel.DepthBand) continue;
                float dx = b.ground.x - a.ground.x;
                float overlap = Feel.BodyGap - Mathf.Abs(dx);
                if (overlap <= 0f) continue;
                float push = (dx != 0f ? Mathf.Sign(dx) : a.facing) * overlap / 2f;
                a.ground.x -= push;
                b.ground.x += push;
            }
    }

    public static Fighter Spawn(int team, string palette, Vector2 pos, int hp, string[] head = null)
    {
        var f = new GameObject(team == 0 ? "Player" : "Enemy").AddComponent<Fighter>();
        f.maxHealth = hp;
        f.Init(team, palette, head ?? Art.HeroHead, pos);
        Fighters.Add(f);
        return f;
    }

    void Awake()
    {
        Time.fixedDeltaTime = 1f / 60f;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        PixelScreen = new RenderTexture(ScreenW, ScreenH, 16) { filterMode = FilterMode.Point };
        cam = Camera.main;
        if (cam == null) cam = new GameObject("Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ScreenH / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.1f, 0.25f);
        cam.targetTexture = PixelScreen;

        // the window itself needs a camera to clear it; this one draws nothing
        var bars = new GameObject("Letterbox").AddComponent<Camera>();
        bars.cullingMask = 0;
        bars.clearFlags = CameraClearFlags.SolidColor;
        bars.backgroundColor = Color.black;
        bars.depth = -100;

        // street: brick wall from the sidewalk up past the top of the screen, street below
        MakeBackdrop(Art.Bricks, new Vector2(1900f, 40f), new Vector2(4200f, 144f), -20000);
        MakeBackdrop(Art.Street, new Vector2(1900f, -74f), new Vector2(4200f, 84f), -20000);
        for (int i = 0; i < 40; i++)
            MakeBackdrop(Art.Window(i % 3 == 1), new Vector2(48f + i * 96f, 50f), new Vector2(24f, 32f), -19999);

        Restart();
    }

    static void MakeBackdrop(Sprite sprite, Vector2 pos, Vector2 size, int order)
    {
        var sr = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = order;
        sr.transform.position = pos;
    }

    void Restart()
    {
        foreach (var f in Fighters) if (f != null) Destroy(f.gameObject);
        Reset();
        wave = 0;
        camX = ScreenW / 2f;
        Hero = Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        Hero.gameObject.AddComponent<Player>();
    }

    void SpawnWave()
    {
        wave++;
        for (int i = 0; i < Mathf.Min(1 + wave, 5); i++)
        {
            // alternate sides, each one further out so they don't stack; no room on the left -> right
            float x = camX + (i % 2 == 0 ? 1f : -1f) * (ScreenW / 2f + 20f + i * 12f);
            if (x < MinX) x = camX + ScreenW / 2f + 20f + i * 12f;
            var pos = new Vector2(x, Random.Range(MinDepth, MaxDepth));
            // punk: quick, fragile; bruiser: slow, takes more hits
            bool bruiser = Random.value < 0.35f;
            var e = bruiser ? Spawn(1, Art.GruntB, pos, 5, Art.BruiserHead) : Spawn(1, Art.GruntA, pos, 3, Art.PunkHead);
            e.gameObject.AddComponent<Enemy>();
            if (bruiser) e.walkX = Feel.EnemyWalkX * 0.7f;
        }
    }

    void FixedUpdate()
    {
        if (RestartRequested) Restart();
        Simulate();

        for (int i = Fighters.Count - 1; i >= 0; i--)
        {
            var f = Fighters[i];
            if (f.team != 0 && f.state == State.Dead && f.stateFrames > Feel.CorpseFrames)
            {
                Fighters.RemoveAt(i);
                Destroy(f.gameObject);
            }
        }
        if (Hero.Alive && !Fighters.Exists(f => f.team != 0)) SpawnWave();

        camX = Mathf.Max(ScreenW / 2f, camX + (Hero.ground.x - camX) * 0.1f);
    }

    void LateUpdate()
    {
        float shake = shakeFrames > 0 ? (Frame % 2 == 0 ? 2f : -2f) : 0f;
        cam.transform.position = new Vector3(Mathf.Round(camX), shake, -10f);
    }

    void OnGUI()
    {
        int scale = Mathf.Max(1, Mathf.Min(Screen.width / ScreenW, Screen.height / ScreenH));
        int w = ScreenW * scale, h = ScreenH * scale;
        var view = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(view, PixelScreen);

        // HUD sized in game pixels, so it scales with the picture
        hud ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        hud.fontSize = 8 * scale;
        Text(view.x + 8 * scale, view.y + 6 * scale, scale, $"HP {Mathf.Max(Hero.health, 0)}    WAVE {wave}");
        if (Hero.state == State.Dead)
            Text(view.x + 72 * scale, view.y + 100 * scale, scale, "GAME OVER - PRESS J");
    }

    GUIStyle hud;

    void Text(float x, float y, int scale, string s)
    {
        hud.normal.textColor = Color.black;
        GUI.Label(new Rect(x + scale, y + scale, 600, 40 * scale), s, hud);
        hud.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 600, 40 * scale), s, hud);
    }
}
