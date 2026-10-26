using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: 60 Hz fixed loop, 256x224 pixel screen, floor, player, enemy waves.
public class Game : MonoBehaviour
{
    public const int ScreenW = 384, ScreenH = 240;
    public const float MinX = 16f, MinDepth = -105f, MaxDepth = 10f; // the street's sidewalk and road

    public static readonly List<Fighter> Fighters = new List<Fighter>();
    public static Fighter Hero;
    public static int Frame;    // every fixed tick
    public static int SimFrame; // ticks that actually simulated (hitstop excluded)
    public static bool RestartRequested;
    public static RenderTexture PixelScreen;
    static int freezeFrames, shakeFrames, flashFrames;

    public static int RequestedStage; // debug jump (keys 1/2)
    const int StreetWaves = 3;

    Camera cam;
    float camX = ScreenW / 2f;
    int stage, stageWave, bannerFrames;
    readonly List<GameObject> backdrop = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    public static void Reset()
    {
        Fighters.Clear();
        Hero = null;
        Frame = SimFrame = freezeFrames = shakeFrames = flashFrames = 0;
        RestartRequested = false;
        RequestedStage = 0;
    }

    public static void Freeze(int frames, int shake)
    {
        freezeFrames = Mathf.Max(freezeFrames, frames);
        shakeFrames = Mathf.Max(shakeFrames, shake);
    }

    public static void Flash(int frames) => flashFrames = Mathf.Max(flashFrames, frames);

    // one 60 Hz tick of the whole fight; hitstop skips it
    public static void Simulate()
    {
        Frame++;
        if (shakeFrames > 0) shakeFrames--;
        if (flashFrames > 0) flashFrames--;
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

    public static Fighter Spawn(int team, string look, Vector2 pos, int hp)
    {
        var f = new GameObject(team == 0 ? "Player" : "Enemy").AddComponent<Fighter>();
        f.maxHealth = hp;
        f.Init(team, look, pos);
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

        Restart();
    }

    void BuildBackdrop()
    {
        foreach (var g in backdrop) Destroy(g);
        backdrop.Clear();
        if (stage == 1)
        {
            // street: brick wall from the sidewalk up past the top of the screen, road below
            // street panorama, bottom on the screen edge; its sidewalk and road are the walkable area
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
            MakeBackdrop(Art.StreetScene, new Vector2(1200f * 2f, 0f), new Vector2(1200f * 4f, 240f), -20000);
        }
        else
        {
            // rooftop at night: sky, skyline behind the ledge, water tanks and neon signs
            MakeBackdrop(Art.NightSky, new Vector2(1900f, 60f), new Vector2(4200f, 144f), -20002);
            MakeBackdrop(Art.Skyline, new Vector2(1900f, 46f), new Vector2(4200f, 72f), -20001);
            MakeBackdrop(Art.Roof, new Vector2(1900f, -56f), new Vector2(4200f, 132f), -20000);
            for (int i = 0; i < 14; i++)
                MakeBackdrop(Art.Tank, new Vector2(150f + i * 300f, 32f), new Vector2(28f, 44f), -19999);
            for (int i = 0; i < 10; i++)
                MakeBackdrop(Art.Neon, new Vector2(300f + i * 400f, 84f), new Vector2(56f, 22f), -19999);
        }
    }

    void MakeBackdrop(Sprite sprite, Vector2 pos, Vector2 size, int order)
    {
        var sr = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = order;
        sr.transform.position = pos;
        backdrop.Add(sr.gameObject);
    }

    void SetStage(int s)
    {
        stage = s;
        stageWave = 0;
        bannerFrames = 150;
        BuildBackdrop();
    }

    void ClearEnemies()
    {
        for (int i = Fighters.Count - 1; i >= 0; i--)
            if (Fighters[i].team != 0)
            {
                Destroy(Fighters[i].gameObject);
                Fighters.RemoveAt(i);
            }
    }

    void Restart()
    {
        foreach (var f in Fighters) if (f != null) Destroy(f.gameObject);
        Reset();
        camX = ScreenW / 2f;
        Hero = Spawn(0, Art.Hero, new Vector2(60f, -50f), 10);
        Hero.gameObject.AddComponent<Player>();
        SetStage(1);
    }

    void SpawnWave()
    {
        stageWave++;
        for (int i = 0; i < Mathf.Min(1 + stageWave, 5); i++)
        {
            // alternate sides, each one further out so they don't stack; no room on the left -> right
            float x = camX + (i % 2 == 0 ? 1f : -1f) * (ScreenW / 2f + 20f + i * 12f);
            if (x < MinX) x = camX + ScreenW / 2f + 20f + i * 12f;
            var pos = new Vector2(x, Random.Range(MinDepth, MaxDepth));

            // rooftop: every other enemy is a whip fighter; others: quick and fragile, or slow and tough
            bool lasher = stage == 2 && i % 2 == 0;
            bool bruiser = !lasher && Random.value < 0.35f;
            var e = lasher ? Spawn(1, Art.Lasher, pos, 4)
                : bruiser ? Spawn(1, Art.GruntB, pos, 5)
                : Spawn(1, Art.GruntA, pos, 3);
            e.whip = lasher;
            e.gameObject.AddComponent<Enemy>();
            if (bruiser) e.walkX = Feel.EnemyWalkX * 0.7f;
        }
    }

    void FixedUpdate()
    {
        if (RestartRequested) Restart();
        if (RequestedStage > 0)
        {
            ClearEnemies();
            SetStage(RequestedStage);
            RequestedStage = 0;
        }
        if (bannerFrames > 0) bannerFrames--;
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
        if (Hero.Alive && !Fighters.Exists(f => f.team != 0))
        {
            if (stage == 1 && stageWave >= StreetWaves) SetStage(2);
            SpawnWave();
        }

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
        if (Event.current.type == EventType.Repaint)
        {
            GUI.DrawTexture(view, PixelScreen);
            if (flashFrames > 0) // white flash on finishers
            {
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.DrawTexture(view, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        // HUD sized in game pixels, so it scales with the picture
        hud ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        hud.fontSize = 8 * scale;
        Text(view.x + 8 * scale, view.y + 6 * scale, scale, $"VASYL  HP {Mathf.Max(Hero.health, 0)}    STAGE {stage}-{stageWave}");
        if (bannerFrames > 0)
            Text(view.x + 60 * scale, view.y + 80 * scale, scale, stage == 1 ? "STAGE 1   THE STREET" : "STAGE 2   NEON ROOFTOP");
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
