using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: 60 Hz fixed loop, 384x240 pixel screen, the ORBIT 9 bar level, player, enemy waves.
public class Game : MonoBehaviour
{
    public const int ScreenW = 384, ScreenH = 240;
    public const float LevelW = 1152f;                                   // three screens of bar
    public const float MinX = 16f, MaxX = LevelW - 16f;
    public const float MinDepth = -105f, MaxDepth = 10f;                 // the bar floor

    public static readonly List<Fighter> Fighters = new List<Fighter>();
    public static readonly List<Prop> Props = new List<Prop>();
    public static Fighter Hero;
    public static int Frame;    // every fixed tick
    public static int SimFrame; // ticks that actually simulated (hitstop excluded)
    public static bool RestartRequested;
    public static bool ShowHelp; // H held
    public static RenderTexture PixelScreen;
    static int freezeFrames, shakeFrames, flashFrames;

    Camera cam;
    float camX = ScreenW / 2f;
    int wave, toSpawn, spawnDelay;

    // layers that scroll at their own speed: < 1 is further away, > 1 is in front of the fighters
    readonly List<(Transform layer, float speed, float x)> parallax = new List<(Transform, float, float)>();
    readonly List<SpriteRenderer> occluders = new List<SpriteRenderer>(); // foreground pillars, faded when someone is behind

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    public static void Reset()
    {
        Fighters.Clear();
        Props.Clear();
        Hero = null;
        Frame = SimFrame = freezeFrames = shakeFrames = flashFrames = 0;
        RestartRequested = false;
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
        for (int i = Props.Count - 1; i >= 0; i--) Props[i].Step(); // backwards: a prop may break and remove itself
        Separate();
        BlockByTables();
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

    // tables are solid on the floor: walk around them or jump over; pushed out along the shallower side
    static void BlockByTables()
    {
        foreach (var t in Props)
        {
            if (t.kind != Prop.Kind.Table || t.vel != Vector2.zero) continue;
            foreach (var f in Fighters)
            {
                if (!f.Solid && f.state != State.Jump || f.height > Feel.TableHeight) continue;
                float dx = f.ground.x - t.ground.x, dy = f.ground.y - t.ground.y;
                float px = Feel.TableHalfWidth - Mathf.Abs(dx), py = Feel.TableHalfDepth - Mathf.Abs(dy);
                if (px <= 0f || py <= 0f) continue;
                if (px < py) f.ground.x += dx >= 0f ? px : -px;
                else f.ground.y += dy >= 0f ? py : -py;
            }
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

    public static Prop SpawnProp(Prop.Kind kind, Vector2 pos, float onHeight = 0f, bool faceLeft = false)
    {
        var p = new GameObject(kind.ToString()).AddComponent<Prop>();
        p.Init(kind, pos, onHeight, faceLeft);
        Props.Add(p);
        return p;
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
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        cam.targetTexture = PixelScreen;

        // the window itself needs a camera to clear it; this one draws nothing
        var bars = new GameObject("Letterbox").AddComponent<Camera>();
        bars.cullingMask = 0;
        bars.clearFlags = CameraClearFlags.SolidColor;
        bars.backgroundColor = Color.black;
        bars.depth = -100;

        BuildBar();
        Restart();
    }

    // ORBIT 9: layers from Resources/Art/Bar, placed by their pixel rows (screen top = y 120, floor from row 112)
    void BuildBar()
    {
        Layer(Art.BarSpace, new Vector2(LevelW / 2f, 120f - 2f - 28f), -20004, 0.5f); // seen through the window
        Layer(Art.BarBack, new Vector2(LevelW / 2f, 120f - 56f), -20003);
        Layer(Art.BarCounter, new Vector2(600f, 120f - 64f - 24f), -20002);
        Layer(Art.BarFloor, new Vector2(LevelW / 2f, 120f - 112f - 64f), -20001);
        foreach (float x in new[] { 250f, 900f })
            occluders.Add(Layer(Art.BarPillar, new Vector2(x, 0f), 30000, 1.25f));     // in front of everyone
    }

    SpriteRenderer Layer(Sprite sprite, Vector2 pos, int order, float speed = 1f)
    {
        var sr = new GameObject("Layer").AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.transform.position = pos;
        if (speed != 1f) parallax.Add((sr.transform, speed, pos.x));
        return sr;
    }

    const float TableTop = 26f;   // height of a table's top surface (table.png)
    const float CounterTop = 47f; // height of the counter top above its front edge (bar_counter.png)
    const float CounterEdge = 9f; // depth of the counter's front edge

    // furniture the way a bar would have it: stools along the counter with drinks on it,
    // and tables on the floor, each with chairs turned towards it and bottles on top
    static void SpawnBarProps()
    {
        foreach (float x in new[] { 380f, 470f, 560f, 650f, 740f, 830f })
            SpawnProp(Prop.Kind.Stool, new Vector2(x, CounterEdge - 5f));
        foreach (float x in new[] { 425f, 612f, 795f })
            SpawnProp(Prop.Kind.Bottle, new Vector2(x, CounterEdge), CounterTop);

        var tables = new (float x, float depth, int bottles)[] { (170f, -45f, 1), (460f, -82f, 2), (700f, -40f, 1), (1000f, -78f, 2) };
        foreach (var (x, depth, bottles) in tables)
        {
            SpawnProp(Prop.Kind.Table, new Vector2(x, depth));
            SpawnProp(Prop.Kind.Chair, new Vector2(x - 28f, depth));
            SpawnProp(Prop.Kind.Chair, new Vector2(x + 28f, depth), faceLeft: true);
            for (int i = 0; i < bottles; i++)
                SpawnProp(Prop.Kind.Bottle, new Vector2(x - 4f + i * 9f, depth), TableTop);
        }
    }

    void Restart()
    {
        foreach (var f in Fighters) if (f != null) Destroy(f.gameObject);
        foreach (var p in Props) if (p != null) Destroy(p.gameObject);
        Reset();
        wave = toSpawn = 0;
        Hero = Spawn(0, Art.Hero, new Vector2(340f, -8f), 10); // at the counter
        Hero.gameObject.AddComponent<Player>();
        camX = CameraX(Hero.ground.x);
        SpawnBarProps();
    }

    static float CameraX(float target) => Mathf.Clamp(target, ScreenW / 2f, LevelW - ScreenW / 2f);

    // a wave walks in one at a time (SpawnGap apart), so nobody pops in on top of anyone
    void SpawnWave()
    {
        wave++;
        toSpawn = Mathf.Min(1 + wave, 5);
        spawnDelay = 0;
    }

    void SpawnNext()
    {
        // through the entrance door or from the stage end, whichever is further from the hero
        const float Door = 60f, StageEnd = LevelW - 40f;
        bool door = Mathf.Abs(Hero.ground.x - Door) > Mathf.Abs(Hero.ground.x - StageEnd);
        var pos = new Vector2(door ? Door : StageEnd, door ? MaxDepth - 4f : Random.Range(MinDepth, MaxDepth));

        // every other enemy is a whip fighter; others: quick and fragile, or slow and tough
        bool lasher = toSpawn % 2 == 1;
        bool bruiser = !lasher && Random.value < 0.35f;
        var e = lasher ? Spawn(1, Art.Lasher, pos, 4)
            : bruiser ? Spawn(1, Art.GruntB, pos, 5)
            : Spawn(1, Art.GruntA, pos, 3);
        e.whip = lasher;
        e.gameObject.AddComponent<Enemy>();
        if (bruiser) e.walkX = Feel.EnemyWalkX * 0.7f;
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
        if (Hero.Alive && toSpawn == 0 && !Fighters.Exists(f => f.team != 0)) SpawnWave();
        if (toSpawn > 0 && --spawnDelay <= 0)
        {
            SpawnNext();
            toSpawn--;
            spawnDelay = Feel.SpawnGap;
        }

        camX += (CameraX(Hero.ground.x) - camX) * 0.1f;
    }

    void LateUpdate()
    {
        float x = Mathf.Round(camX);
        float shake = shakeFrames > 0 ? (Frame % 2 == 0 ? 2f : -2f) : 0f;
        cam.transform.position = new Vector3(x, shake, -10f);
        // a layer at speed s moves s times as fast as the floor; authored positions match the start of the level
        foreach (var (layer, speed, baseX) in parallax)
            layer.position = new Vector3(Mathf.Round(baseX + (x - ScreenW / 2f) * (1f - speed)), layer.position.y, 0f);
        foreach (var pillar in occluders)
        {
            bool behind = Fighters.Exists(f => Mathf.Abs(f.ground.x - pillar.transform.position.x) < 24f);
            pillar.color = new Color(1f, 1f, 1f, behind ? 0.35f : 1f);
        }
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
        if (Event.current.type == EventType.Repaint) // dark strip so the text reads over any background
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(view.x, view.y, view.width, 20 * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        Text(view.x + 8 * scale, view.y + 6 * scale, scale, $"VASYL  HP {Mathf.Max(Hero.health, 0)}");
        if (ShowHelp || Frame < 8 * 60) // controls for the first seconds, then while H is held
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(view.x, view.y + 186 * scale, view.width, 54 * scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Text(view.x + 8 * scale, view.y + 190 * scale, scale, "WASD MOVE   J PUNCH   L KICK   K JUMP   S+L STOMP");
            Text(view.x + 8 * scale, view.y + 206 * scale, scale, "TAP TAP = RUN   RUN+J FLYING KICK   RUN+L BICYCLE KICK");
            Text(view.x + 8 * scale, view.y + 222 * scale, scale, "J BY A CHAIR OR BOTTLE = PICK UP      HOLD H = HELP");
        }
        else if (Hero.held != null)
            Text(view.x + 8 * scale, view.y + 224 * scale, scale, "J - SWING      L - THROW");
        if (Hero.state == State.Dead)
            Text(view.x + 120 * scale, view.y + 100 * scale, scale, "GAME OVER - PRESS J");
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
