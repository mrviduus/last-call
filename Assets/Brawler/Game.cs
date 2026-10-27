using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: 60 Hz fixed loop, 384x240 pixel screen, the OLD LAMP pub level, player, enemy waves.
public class Game : MonoBehaviour
{
    public const int ScreenW = 384, ScreenH = 240;
    public const float LevelW = 1152f;                                   // three screens of bar
    public const float MinX = 16f, MaxX = LevelW - 16f;
    public const float MinDepth = -105f, MaxDepth = 10f;                 // the bar floor

    public static readonly List<Fighter> Fighters = new List<Fighter>();
    public static readonly List<Prop> Props = new List<Prop>();
    public static readonly List<Lamp> Lamps = new List<Lamp>();
    public static Fighter Hero;
    public static Fighter LastHit; // the enemy the hero hit last: its health bar is in the HUD
    public static int Frame;    // every fixed tick
    public static int SimFrame; // ticks that actually simulated (hitstop excluded)
    public static bool RestartRequested;
    public static bool Brawl; // the hero has thrown the first punch: patrons get up, waves come in
    public static bool ShowHelp; // H held
    public static int Wave;      // waves so far; later ones bring the drunk and the bottle thrower
    public static int Clock;     // ticks since the first punch; at Feel.LastCallFrames it is midnight
    public static bool Midnight; // the bell has rung: the closer is coming, no more waves after his
    public static bool Won;      // midnight, and everyone is down
    public static int Score, HeroCombo; // combo: the hero's hits in a row, until he's hit or stops hitting
    static int comboTimer;
    public static RenderTexture PixelScreen;
    static int freezeFrames, shakeFrames, flashFrames, slowMoFrames;
    static readonly List<Prop> stepping = new List<Prop>();
    public static bool Frozen => freezeFrames > 0;

    Camera cam;
    float camX = ScreenW / 2f;
    int toSpawn, spawnDelay;

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
        Lamps.Clear();
        Prop.parts.Clear();
        Puddle.Clear();
        Fire.Clear();
        Hero = LastHit = null;
        Wave = Clock = Score = HeroCombo = comboTimer = 0;
        Midnight = Won = false;
        Frame = SimFrame = freezeFrames = shakeFrames = flashFrames = slowMoFrames = 0;
        Time.timeScale = 1f;
        RestartRequested = false;
        Brawl = false;
    }

    public static void Freeze(int frames, int shake)
    {
        freezeFrames = Mathf.Max(freezeFrames, frames);
        shakeFrames = Mathf.Max(shakeFrames, shake);
    }

    // a hero hit landed: the combo goes on, each hit scores more
    public static void Scored()
    {
        HeroCombo++;
        comboTimer = Feel.ComboFrames;
        Score += 10 * HeroCombo;
    }

    public static void Flash(int frames) => flashFrames = Mathf.Max(flashFrames, frames);

    // the whole game in slow motion for a moment (counted in rendered frames)
    public static void SlowMo(int frames) => slowMoFrames = frames;

    void Update()
    {
        Time.timeScale = slowMoFrames > 0 ? Feel.SlowMoScale : 1f;
        if (slowMoFrames > 0) slowMoFrames--;
    }

    // one 60 Hz tick of the whole fight; hitstop skips it
    public static void Simulate()
    {
        Frame++;
        if (shakeFrames > 0) shakeFrames--;
        if (flashFrames > 0) flashFrames--;
        if (freezeFrames > 0) { freezeFrames--; return; }
        SimFrame++;
        foreach (var l in Lamps) l.Step(); // before the fighters, so whoever hangs on one moves with it this frame
        for (int i = 0; i < Fighters.Count; i++) Fighters[i].Step();
        // a snapshot: a prop may break and remove itself (and the table it hit), each steps once
        stepping.Clear();
        stepping.AddRange(Props);
        foreach (var p in stepping) if (Props.Contains(p)) p.Step();
        Puddle.StepAll();
        Fire.StepAll();
        if (comboTimer > 0 && --comboTimer == 0) HeroCombo = 0;
        Separate();
        BlockByTables();
        if (Hero != null) Hero.ResolveStrike(); // the hero wins ties
        for (int i = 0; i < Fighters.Count; i++) if (Fighters[i] != Hero) Fighters[i].ResolveStrike();
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
                // the lighter one gives way more
                float push = (dx != 0f ? Mathf.Sign(dx) : a.facing) * overlap / (a.mass + b.mass);
                a.ground.x = Mathf.Clamp(a.ground.x - push * b.mass, MinX, MaxX);
                b.ground.x = Mathf.Clamp(b.ground.x + push * a.mass, MinX, MaxX);
            }
    }

    // tables and pool tables are solid on the floor: walk around them or jump over (low ones);
    // pushed out along the shallower side
    static void BlockByTables()
    {
        foreach (var t in Props)
        {
            if (!t.Blocks) continue;
            foreach (var f in Fighters)
            {
                if (!f.Solid && f.state != State.Jump || f.height > t.BlockHeight) continue;
                float dx = f.ground.x - t.ground.x, dy = f.ground.y - t.ground.y;
                float px = t.HalfWidth - Mathf.Abs(dx), py = t.HalfDepth - Mathf.Abs(dy);
                if (px <= 0f || py <= 0f) continue;
                float x = f.ground.x + (dx >= 0f ? px : -px);
                if (px < py && x >= MinX && x <= MaxX) f.ground.x = x;
                else f.ground.y = Mathf.Clamp(f.ground.y + (dy >= 0f ? py : -py), MinDepth, MaxDepth); // cornered by the wall: out sideways
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

    // OLD LAMP: layers from Resources/Art/Bar, placed by their pixel rows (screen top = y 120, floor from row 112)
    void BuildBar()
    {
        Layer(Art.BarView, new Vector2(LevelW / 2f, 120f - 2f - 24f), -20004, 0.5f);  // the street seen through the windows
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
    const float FeltTop = 28f;    // height of the pool table's felt (pool_table.png)
    const float CounterTop = 47f; // height of the counter top above its front edge (bar_counter.png)
    const float CounterEdge = 9f; // depth of the counter's front edge
    const float StoolSeat = 10f;  // a bar stool's seat is this much higher than a chair's (stool.png vs chair.png)

    // furniture the way a bar would have it: stools along the counter with drinks on it,
    // and tables on the floor, each with chairs turned towards it and bottles on top
    static void SpawnBarProps()
    {
        var stools = new List<Prop>();
        foreach (float x in new[] { 380f, 470f, 560f, 650f, 740f, 830f })
            stools.Add(SpawnProp(Prop.Kind.Stool, new Vector2(x, CounterEdge - 5f)));
        // a couple chatting at the counter, up on two neighbouring stools
        Seat(Art.GuyDenim, stools[3], 1, Feel.GuyHealth, StoolSeat);
        Seat(Art.GruntB, stools[4], -1, Feel.GirlHealth, StoolSeat);
        foreach (float x in new[] { 425f, 612f, 795f })
            SpawnProp(Prop.Kind.Bottle, new Vector2(x, CounterEdge), CounterTop);

        // pool tables, each with its balls and a cue on the felt (towards the front rail, where you can reach them)
        // and a lamp hanging over it; the floor art has the lamp's light pool at the same spot
        Color[] ballColours = { new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.45f, 1f), new Color(1f, 0.3f, 0.25f),
            new Color(0.6f, 0.3f, 0.9f), new Color(1f, 1f, 1f) };
        foreach (var (x, depth) in new[] { (620f, -82f), (1010f, -60f) })
        {
            SpawnProp(Prop.Kind.PoolTable, new Vector2(x, depth));
            for (int i = 0; i < ballColours.Length; i++)
                SpawnProp(Prop.Kind.Ball, new Vector2(x - 30f + i * 13f, depth - 9f), FeltTop).Tint(ballColours[i]);
            SpawnProp(Prop.Kind.Cue, new Vector2(x, depth - 9f), FeltTop + 2f);
            Lamp.Spawn(x, depth, Feel.LampBottom);
        }

        // a couple at each round table: a guy on the left chair, a girl on the right, facing each other
        var tables = new (float x, float depth, int bottles)[] { (170f, -45f, 1), (400f, -88f, 2), (790f, -30f, 1) };
        for (int t = 0; t < tables.Length; t++)
        {
            var (x, depth, bottles) = tables[t];
            SpawnProp(Prop.Kind.Table, new Vector2(x, depth));
            var left = SpawnProp(Prop.Kind.Chair, new Vector2(x - 28f, depth));
            var right = SpawnProp(Prop.Kind.Chair, new Vector2(x + 28f, depth), faceLeft: true);
            for (int i = 0; i < bottles; i++)
                SpawnProp(Prop.Kind.Bottle, new Vector2(x - 4f + i * 9f, depth), TableTop);
            Seat(t % 2 == 0 ? Art.Guy : Art.GuyDenim, left, 1, Feel.GuyHealth);
            Seat(t % 2 == 0 ? Art.GruntA : Art.GruntB, right, -1, Feel.GirlHealth);
        }
    }

    public static Fighter Seat(string look, Prop chair, int faceDir, int hp, float seatHeight = 0f)
    {
        var f = Spawn(1, look, chair.ground, hp);
        f.Sit(faceDir, seatHeight);
        chair.seatedBy = f;
        f.gameObject.AddComponent<Enemy>().Setup(wakeDelay: Random.Range(20, 140));
        return f;
    }

    void Restart()
    {
        foreach (var f in Fighters) if (f != null) Destroy(f.gameObject);
        foreach (var p in Props) if (p != null) Destroy(p.gameObject);
        foreach (var l in Lamps) if (l != null) Destroy(l.gameObject);
        Reset();
        Drop.Clear();
        toSpawn = 0;
        Hero = Spawn(0, Art.Hero, new Vector2(340f, -8f), Feel.HeroHealth); // at the counter
        Hero.gameObject.AddComponent<Player>();
        camX = CameraX(Hero.ground.x);
        SpawnBarProps();
    }

    static float CameraX(float target) => Mathf.Clamp(target, ScreenW / 2f, LevelW - ScreenW / 2f);

    // a wave walks in one at a time (SpawnGap apart), so nobody pops in on top of anyone
    // until midnight a new wave comes in whenever the bar is clear
    void SpawnWave()
    {
        Wave++;
        toSpawn = Mathf.Min(1 + Wave, 5);
        spawnDelay = 0;
    }

    // midnight: the bell rings and the closer walks in with two of his own, whoever is still standing
    void CallMidnight()
    {
        Midnight = true;
        Sfx.Play("bell");
        toSpawn = 3;
        spawnDelay = 0;
    }

    void SpawnNext()
    {
        // through the entrance door or from the stage end, whichever is further from the hero
        const float Door = 60f, StageEnd = LevelW - 40f;
        bool door = Mathf.Abs(Hero.ground.x - Door) > Mathf.Abs(Hero.ground.x - StageEnd);
        var pos = new Vector2(door ? Door : StageEnd, door ? MaxDepth - 4f : Random.Range(MinDepth, MaxDepth));

        // guys and girls take turns; every third one is a whip fighter; from wave 2 the last one in is a drunk,
        // from wave 3 a bottle thrower comes too; the boss leads the last wave
        bool boss = Midnight && toSpawn == 3;
        bool drunk = !boss && Wave >= 2 && toSpawn == 1, chucker = !boss && Wave >= 3 && toSpawn == 4;
        bool lasher = !boss && !drunk && !chucker && toSpawn % 3 == 0, guy = !lasher && toSpawn % 2 == 0;
        var e = boss ? Spawn(1, Art.Boss, pos, Feel.BossHealth)
            : drunk ? Spawn(1, Art.Drunk, pos, Feel.DrunkHealth)
            : chucker ? Spawn(1, Art.Chucker, pos, Feel.ChuckerHealth)
            : lasher ? Spawn(1, Art.Lasher, pos, Feel.GuyHealth)
            : guy ? Spawn(1, Random.value < 0.5f ? Art.Guy : Art.GuyDenim, pos, Feel.GuyHealth)
            : Spawn(1, Random.value < 0.5f ? Art.GruntA : Art.GruntB, pos, Feel.GirlHealth);
        e.whip = lasher;
        e.gameObject.AddComponent<Enemy>().Setup();
        e.GetComponent<Enemy>().chucker = chucker;
        if (guy || drunk) e.walkX = Feel.EnemyWalkX * 0.85f; // heavier, a bit slower
        if (drunk) e.drunk = true;
        if (boss) { e.armored = true; e.mass = Feel.BossMass; e.size = 1.25f; }
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
        if (Brawl && !Midnight && Hero.Alive && ++Clock >= Feel.LastCallFrames) CallMidnight();
        if (Brawl && !Won && Hero.Alive && toSpawn == 0 && !Fighters.Exists(f => f.team != 0))
        {
            if (Midnight) Won = true;
            else SpawnWave();
        }
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
            bool behind = false;
            foreach (var f in Fighters) behind |= Mathf.Abs(f.ground.x - pillar.transform.position.x) < 24f;
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
        Text(view.x + 8 * scale, view.y + 6 * scale, scale, "BRICK");
        Bar(view.x + 44 * scale, view.y + 8 * scale, 100 * scale, 6 * scale, Hero);
        if (LastHit != null) // the enemy you're fighting, Streets of Rage style
        {
            Text(view.x + 200 * scale, view.y + 6 * scale, scale, Art.Name(LastHit.look));
            Bar(view.x + 270 * scale, view.y + 8 * scale, 100 * scale * LastHit.maxHealth / Feel.HeroHealth, 6 * scale, LastHit);
        }
        // the clock over the bar: 11:59:00 at the first punch, midnight is last call
        Text(view.x + 8 * scale, view.y + 22 * scale, scale, (Midnight ? "12:00 AM" : "11:59:" + (Clock / 60).ToString("00") + " PM")
            + "   SCORE " + Score
            + (HeroCombo >= 2 ? "   " + HeroCombo + " HITS" : ""));
        if (Frame < Feel.IntroFrames && !Won)
        {
            Text(view.x + 96 * scale, view.y + 70 * scale, scale, "11:59 PM. THE OLD LAMP.");
            Text(view.x + 96 * scale, view.y + 84 * scale, scale, "BRICK CAME FOR ONE LAST PINT.");
            Text(view.x + 96 * scale, view.y + 98 * scale, scale, "AT MIDNIGHT THE GANG CLOSES IT.");
        }
        if (Won)
        {
            Text(view.x + 120 * scale, view.y + 80 * scale, scale, "MIDNIGHT. LAST CALL.");
            Text(view.x + 120 * scale, view.y + 94 * scale, scale, "THE CLOSER IS DOWN. YOU STAYED.");
            Text(view.x + 120 * scale, view.y + 108 * scale, scale, "SCORE " + Score + " - PRESS J");
        }
        if (ShowHelp || Frame < 8 * 60) // controls for the first seconds, then while H is held
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(view.x, view.y + 170 * scale, view.width, 70 * scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Text(view.x + 8 * scale, view.y + 174 * scale, scale, "WASD MOVE  J PUNCH  L KICK  K JUMP  J+K SPECIAL  J+L DIRTY  H HELP");
            Text(view.x + 8 * scale, view.y + 190 * scale, scale, "TAP TAP = RUN   RUN+J FLYING KICK   RUN+L BICYCLE KICK");
            Text(view.x + 8 * scale, view.y + 206 * scale, scale, "WALK INTO A DAZED ONE: J HEADBUTT  K THROW  L RIP ARM");
            Text(view.x + 8 * scale, view.y + 222 * scale, scale, "S+L STOMP, THEN L: KICK THE HEAD OFF   J BY A PROP: PICK UP");
        }
        else if (Hero.held != null)
            Text(view.x + 8 * scale, view.y + 224 * scale, scale, "J - SWING      L - THROW");
        if (Hero.state == State.Dead)
            Text(view.x + 120 * scale, view.y + 100 * scale, scale, "GAME OVER - PRESS J");
    }

    GUIStyle hud;

    // health bar: black frame, dark red back, yellow fill, green for health the special spent and you can win back
    static void Bar(float x, float y, float w, float h, Fighter f)
    {
        if (Event.current.type != EventType.Repaint) return;
        float full = Mathf.Clamp01((float)f.health / f.maxHealth);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);
        GUI.color = new Color(0.35f, 0.08f, 0.08f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.82f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, w * full, h), Texture2D.whiteTexture);
        if (f.recoverable > 0) // the special's cost, still to be won back
        {
            GUI.color = new Color(0.35f, 0.85f, 0.35f);
            GUI.DrawTexture(new Rect(x + w * full, y, w * f.recoverable / f.maxHealth, h), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void Text(float x, float y, int scale, string s)
    {
        hud.normal.textColor = Color.black;
        GUI.Label(new Rect(x + scale, y + scale, ScreenW * scale, 40 * scale), s, hud);
        hud.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, ScreenW * scale, 40 * scale), s, hud);
    }
}
