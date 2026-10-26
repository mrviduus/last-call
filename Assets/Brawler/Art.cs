using System.Collections.Generic;
using UnityEngine;

// Fighters: "Streets of Fight" by ansimuz, CC0 (see CREDITS.txt), recoloured per look (hair and clothes only).
// The bar is PNGs from Tools/make_bar_art.py; Vasyl's frames and the sitting frames from Tools/make_vasyl_frames.py;
// the whip is drawn in code. 1 texel = 1 screen pixel.
public static class Art
{
    public const string Hero = "Vasyl";          // blond, leather vest
    public const string Guy = "Punk";            // red mohawk, red vest
    public const string GuyDenim = "PunkDenim";  // black hair, denim vest
    public const string GruntA = "Girl";         // red outfit
    public const string GruntB = "GirlBlue";     // brown hair, blue outfit
    public const string Lasher = "GirlBlack";    // purple hair, black leather: the whip fighter

    // a look = which sheet (girl or the male punk) + new colours for its three reds, which both sheets use for
    // hair (top of the body) and clothes (below); null keeps the original. Skin and boots are never touched.
    static readonly Dictionary<string, (bool girl, uint[] hair, uint[] clothes)> Looks =
        new Dictionary<string, (bool, uint[], uint[])>
        {
            ["Vasyl"] = (false, new[] { 0xf0d070u, 0xfff0a0u, 0xb08830u }, new[] { 0x4a3a32u, 0x6a5248u, 0x2a201cu }),
            ["Punk"] = (false, null, null),
            ["PunkDenim"] = (false, new[] { 0x2a2a2eu, 0x4a4a52u, 0x16161au }, new[] { 0x3a5a8au, 0x5a7aaau, 0x2a3a5au }),
            ["Girl"] = (true, null, null),
            ["GirlBlue"] = (true, new[] { 0x5a3a2au, 0x7a5038u, 0x3a2418u }, new[] { 0x2a5ab0u, 0x4a8ae0u, 0x1a3470u }),
            ["GirlBlack"] = (true, new[] { 0x8a3ab0u, 0xb05ae0u, 0x5a1a7au }, new[] { 0x2a2a34u, 0x4a4a58u, 0x16161cu }),
        };
    static readonly int[] SourceReds = { 0xce2038, 0xed4924, 0x8a0b41 }; // red, light red, dark red

    public class Frames
    {
        public Sprite[] Idle, Walk, Throw, Bicycle, Sinking;
        public Sprite Punch1, Punch2, KickWind, Kick, KickRecover, JumpKick, Hurt, Fall, Down, GetUp, Crouch, Air, WindUp, Headbutt;
        public Sprite ElbowRise, ElbowDrop, Sit;
    }

    const int FrameW = 96; // every sheet is a row of 96 px wide frames
    static readonly Dictionary<string, Frames> cache = new Dictionary<string, Frames>();
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static Frames For(string look)
    {
        if (cache.TryGetValue(look, out var f)) return f;
        var (girl, hair, clothes) = Looks[look];
        f = girl ? Girl(hair, clothes) : Punk(look == Hero, hair, clothes);
        f.Sinking = Sinking(f.Hurt);
        return cache[look] = f;
    }

    // full move set
    static Frames Girl(uint[] hair, uint[] clothes)
    {
        var pivot = FeetPivot(Load("Girl/idle"), FrameW);
        Sprite[] S(string sheet) => Sheet("Girl/" + sheet, pivot, hair, clothes, hairRows: 12);
        var jab = S("jab"); var punch = S("punch"); var kick = S("kick"); var jump = S("jump");
        var jumpKick = S("jump_kick"); var dive = S("dive_kick"); var hurt = S("hurt");
        return new Frames
        {
            Idle = S("idle"), Walk = S("walk"),
            Punch1 = jab[1], Punch2 = punch[1], KickWind = kick[1], Kick = kick[3], KickRecover = kick[4], JumpKick = dive[3],
            Hurt = hurt[0], Fall = hurt[1], Down = LyingDown(hurt[1]),
            GetUp = jump[0], Crouch = jump[0], Air = jump[2],
            WindUp = punch[0], Headbutt = jab[2], Throw = new[] { punch[0], punch[2] },
            Bicycle = new[] { kick[3], jumpKick[2] },
            ElbowRise = jump[1], ElbowDrop = hurt[1], Sit = S("sit")[0],
        };
    }

    // only idle, walk, punch and hurt; the last hurt frame lies on the ground
    // the punk's sheets face left (he was drawn as an enemy); mirrored so every character faces right
    static Frames Punk(bool asVasyl, uint[] hair, uint[] clothes)
    {
        var raw = FeetPivot(Load("Punk/idle"), FrameW);
        var pivot = new Vector2(1f - raw.x, raw.y); // the sheets are mirrored below
        Sprite[] S(string sheet) => Sheet("Punk/" + sheet, pivot, hair, clothes, hairRows: 10, mirror: true);
        var idle = S("idle"); var punch = S("punch"); var hurt = S("hurt");
        // Vasyl has his own frames (Tools/make_vasyl_frames.py): knee up, kick, high kick, jump kick; elbow drop
        var kick = asVasyl ? Sheet("Vasyl/kick", pivot) : new[] { punch[1], punch[2], punch[2], punch[2] };
        var elbow = asVasyl ? Sheet("Vasyl/elbow", pivot) : new[] { idle[0], hurt[2] };
        return new Frames
        {
            Idle = idle, Walk = S("walk"),
            Punch1 = punch[2], Punch2 = punch[2], KickWind = kick[0], Kick = kick[1], KickRecover = idle[0], JumpKick = kick[3],
            Hurt = hurt[1], Fall = hurt[2], Down = hurt[3],
            GetUp = idle[0], Crouch = idle[0], Air = idle[0],
            WindUp = punch[1], Headbutt = punch[2], Throw = new[] { punch[1], punch[2] },
            Bicycle = new[] { kick[1], kick[2] },
            ElbowRise = elbow[0], ElbowDrop = elbow[1], Sit = S("sit")[0],
        };
    }

    static Texture2D Load(string path) => Resources.Load<Texture2D>("Art/" + path);

    static Sprite[] Sheet(string path, Vector2 pivot, uint[] hair = null, uint[] clothes = null, int hairRows = 0, bool mirror = false)
    {
        var tex = Load(path);
        if (hair != null) tex = Recolor(tex, hair, clothes, hairRows);
        if (mirror) tex = Mirror(tex);
        var frames = new Sprite[tex.width / FrameW];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(tex, new Rect(i * FrameW, 0, FrameW, tex.height), pivot, 1f, 0, SpriteMeshType.FullRect);
        return frames;
    }

    // the sheets use the same reds for hair and clothes: split by height, hair being the top rows of each frame's body
    static Texture2D Recolor(Texture2D src, uint[] hair, uint[] clothes, int hairRows)
    {
        var px = src.GetPixels32();
        for (int f = 0; f < src.width / FrameW; f++)
        {
            int top = -1; // highest opaque row of this frame (rows count up from the bottom)
            for (int y = src.height - 1; y >= 0 && top < 0; y--)
                for (int x = f * FrameW; x < (f + 1) * FrameW; x++)
                    if (px[y * src.width + x].a > 0) { top = y; break; }
            for (int y = 0; y < src.height; y++)
                for (int x = f * FrameW; x < (f + 1) * FrameW; x++)
                {
                    ref var c = ref px[y * src.width + x];
                    int i = c.a > 0 ? System.Array.IndexOf(SourceReds, c.r << 16 | c.g << 8 | c.b) : -1;
                    if (i >= 0) c = C((y > top - hairRows ? hair : clothes)[i]);
                }
        }
        var tex = new Texture2D(src.width, src.height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // flips every 96 px frame left-right in place
    static Texture2D Mirror(Texture2D src)
    {
        var px = src.GetPixels32();
        var outPx = new Color32[px.Length];
        for (int y = 0; y < src.height; y++)
            for (int x = 0; x < src.width; x++)
            {
                int frame = x / FrameW * FrameW;
                outPx[y * src.width + x] = px[y * src.width + frame + FrameW - 1 - (x - frame)];
            }
        var tex = new Texture2D(src.width, src.height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(outPx);
        tex.Apply();
        return tex;
    }

    // pivot under the feet: lowest opaque row, centred on the body (first frame of the sheet)
    static Vector2 FeetPivot(Texture2D tex, int frameW)
    {
        var px = tex.GetPixels32();
        int minX = frameW, maxX = -1, bottom = tex.height;
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < frameW; x++)
                if (px[y * tex.width + x].a > 0)
                {
                    bottom = Mathf.Min(bottom, y);
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                }
        return new Vector2((minX + maxX + 1) / 2f / frameW, bottom / (float)tex.height);
    }

    // a frame turned 90 degrees counter-clockwise: lying on the back, head behind (no lying frame in the sheet)
    static Sprite LyingDown(Sprite standing)
    {
        var r = standing.rect;
        int w = (int)r.width, h = (int)r.height;
        var src = standing.texture.GetPixels((int)r.x, (int)r.y, w, h);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[x * h + (h - 1 - y)] = src[y * w + x];
        var tex = new Texture2D(h, w) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, h, w), FeetPivot(tex, h), 1f, 0, SpriteMeshType.FullRect);
    }

    // the hurt pose with its bottom rows cut off, deeper each frame: a body hammered into the ground
    static Sprite[] Sinking(Sprite hurt)
    {
        var frames = new Sprite[(int)(Feel.BurySink / 3f) + 1];
        var r = hurt.rect;
        var pivot = new Vector2(hurt.pivot.x / r.width, 0f);
        float soles = hurt.pivot.y; // empty rows under the feet: the first frame stands exactly on the floor
        for (int i = 0; i < frames.Length; i++)
        {
            float cut = soles + i * 3f;
            frames[i] = Sprite.Create(hurt.texture, new Rect(r.x, r.y + cut, r.width, r.height - cut),
                pivot, 1f, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    static Sprite FromResource(string path, Vector2? pivot = null)
    {
        var tex = Load(path);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? Center, 1f, 0, SpriteMeshType.FullRect);
    }

    // f(x, y) with y counted from the top
    public static Sprite Paint(int w, int h, System.Func<int, int, Color32> f, Vector2 pivot)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(h - 1 - y) * w + x] = f(x, y);
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
    }

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static Color32 C(uint rgb, byte a = 255) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);
    static Color32 Mix(uint a, uint b, float t) => Color32.Lerp(C(a), C(b), t);
    static bool Speckle(int x, int y) => ((x * 73856093) ^ (y * 19349663)) % 7 == 0;

    static Sprite shadow, whip;

    // ORBIT 9 bar, drawn by Tools/make_bar_art.py: separate layers and props (redraw the PNGs freely)
    static Sprite barView, barBack, barCounter, barFloor, barPillar, chair, stool, bottle, table, tableFlipped, shard;
    static Sprite poolTable, poolLamp, lampShade, cue, ball;
    static readonly Vector2 Feet = new Vector2(0.5f, 0f);
    public static Sprite BarView => barView != null ? barView : barView = FromResource("Bar/bar_view");
    public static Sprite BarBack => barBack != null ? barBack : barBack = FromResource("Bar/bar_back");
    public static Sprite BarCounter => barCounter != null ? barCounter : barCounter = FromResource("Bar/bar_counter");
    public static Sprite BarFloor => barFloor != null ? barFloor : barFloor = FromResource("Bar/bar_floor");
    public static Sprite BarPillar => barPillar != null ? barPillar : barPillar = FromResource("Bar/bar_pillar");
    public static Sprite Chair => chair != null ? chair : chair = FromResource("Bar/chair", Feet);
    public static Sprite Bottle => bottle != null ? bottle : bottle = FromResource("Bar/bottle", Feet);
    public static Sprite Stool => stool != null ? stool : stool = FromResource("Bar/stool", Feet);
    public static Sprite Table => table != null ? table : table = FromResource("Bar/table", Feet);
    public static Sprite PoolTable => poolTable != null ? poolTable : poolTable = FromResource("Bar/pool_table", Feet);
    public static Sprite PoolLamp => poolLamp != null ? poolLamp : poolLamp = FromResource("Bar/pool_lamp", new Vector2(0.5f, 1f)); // swings from the ceiling
    public static Sprite LampShade => lampShade != null ? lampShade : lampShade = FromResource("Bar/lamp_shade", Feet);
    public static Sprite Cue => cue != null ? cue : cue = FromResource("Bar/cue");
    public static Sprite Ball => ball != null ? ball : ball = FromResource("Bar/ball", Feet);
    public static Sprite TableFlipped => tableFlipped != null ? tableFlipped : tableFlipped = FromResource("Bar/table_flipped", Feet);
    // 2x2 white chip, tinted per shard
    public static Sprite Shard => shard != null ? shard : shard = Paint(2, 2, (x, y) => C(0xffffff), Center);

    public static Sprite Shadow => shadow != null ? shadow : shadow = FromResource("shadow");

    // pivot at the handle end; flip by negative x scale
    public static Sprite Whip => whip != null ? whip : whip = Paint(44, 3, (x, y) =>
        x >= 40 ? C(0xe0c080) : y == 1 ? C(0x6a3a1c) : Clear, new Vector2(0f, 0.5f));

}
