using System.Collections.Generic;
using UnityEngine;

// Fighters: "Streets of Fight" by ansimuz, CC0 (see CREDITS.txt). Variants are tinted copies.
// The bar is PNGs from Tools/make_bar_art.py; Vasyl's own frames from Tools/make_vasyl_frames.py; the whip is drawn in code. 1 texel = 1 screen pixel.
public static class Art
{
    // a look is "<character>" or "<character>:<tint>"; characters are folders under Resources/Art
    public const string Hero = "Vasyl";          // the punk recoloured: blond hair, leather vest (until his own frames)
    public const string GruntA = "Girl";          // street fighter
    public const string GruntB = "Girl:c8a0ff";   // tougher fighter, violet tint
    public const string Lasher = "Girl:b0ffd8";   // whip fighter, teal tint

    public static Color Tint(string look)
    {
        int i = look.IndexOf(':');
        return i >= 0 && ColorUtility.TryParseHtmlString("#" + look.Substring(i + 1), out var c) ? c : Color.white;
    }

    public class Frames
    {
        public Sprite[] Idle, Walk, Throw, Bicycle, Sinking;
        public Sprite Punch1, Punch2, KickWind, Kick, KickRecover, JumpKick, Hurt, Fall, Down, GetUp, Crouch, Air, WindUp, Headbutt;
        public Sprite ElbowRise, ElbowDrop;
    }

    const int FrameW = 96; // every sheet is a row of 96 px wide frames
    static readonly Dictionary<string, Frames> cache = new Dictionary<string, Frames>();
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static Frames For(string look)
    {
        string name = look.Split(':')[0];
        if (cache.TryGetValue(name, out var f)) return f;
        f = name == "Girl" ? Girl() : Punk(name == "Vasyl");
        f.Sinking = Sinking(f.Hurt);
        return cache[name] = f;
    }

    // full move set
    static Frames Girl()
    {
        var pivot = FeetPivot(Load("Girl/idle"), FrameW);
        Sprite[] S(string sheet) => Sheet("Girl/" + sheet, pivot);
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
            ElbowRise = jump[1], ElbowDrop = hurt[1],
        };
    }

    // only idle, walk, punch and hurt; the last hurt frame lies on the ground
    // the punk's sheets face left (he was drawn as an enemy); mirrored so every character faces right
    static Frames Punk(bool asVasyl)
    {
        var raw = FeetPivot(Load("Punk/idle"), FrameW);
        var pivot = new Vector2(1f - raw.x, raw.y); // the sheets are mirrored below
        Sprite[] S(string sheet) => Sheet("Punk/" + sheet, pivot, asVasyl, mirror: true);
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
            ElbowRise = elbow[0], ElbowDrop = elbow[1],
        };
    }

    static Texture2D Load(string path) => Resources.Load<Texture2D>("Art/" + path);

    static Sprite[] Sheet(string path, Vector2 pivot, bool asVasyl = false, bool mirror = false)
    {
        var tex = asVasyl ? RecolorAsVasyl(Load(path)) : Load(path);
        if (mirror) tex = Mirror(tex);
        var frames = new Sprite[tex.width / FrameW];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(tex, new Rect(i * FrameW, 0, FrameW, tex.height), pivot, 1f, 0, SpriteMeshType.FullRect);
        return frames;
    }

    // the punk's reds, by where they are: hair at the top of the body, vest below it
    static readonly Dictionary<int, Color32> BlondHair = new Dictionary<int, Color32>
    {
        [0xce2038] = C(0xf0d070), [0xed4924] = C(0xfff0a0), [0x8a0b41] = C(0xb08830),
    };
    static readonly Dictionary<int, Color32> LeatherVest = new Dictionary<int, Color32>
    {
        [0xce2038] = C(0x4a3a32), [0xed4924] = C(0x6a5248), [0x8a0b41] = C(0x2a201c),
    };
    const int HairRows = 10; // rows from the top of each frame's body that are hair

    static Texture2D RecolorAsVasyl(Texture2D src)
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
                    var map = y > top - HairRows ? BlondHair : LeatherVest;
                    if (c.a > 0 && map.TryGetValue(c.r << 16 | c.g << 8 | c.b, out var to)) c = to;
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
    static Sprite barSpace, barBack, barCounter, barFloor, barPillar, chair, stool, bottle, table, tableFlipped, shard;
    static readonly Vector2 Feet = new Vector2(0.5f, 0f);
    public static Sprite BarSpace => barSpace != null ? barSpace : barSpace = FromResource("Bar/bar_space");
    public static Sprite BarBack => barBack != null ? barBack : barBack = FromResource("Bar/bar_back");
    public static Sprite BarCounter => barCounter != null ? barCounter : barCounter = FromResource("Bar/bar_counter");
    public static Sprite BarFloor => barFloor != null ? barFloor : barFloor = FromResource("Bar/bar_floor");
    public static Sprite BarPillar => barPillar != null ? barPillar : barPillar = FromResource("Bar/bar_pillar");
    public static Sprite Chair => chair != null ? chair : chair = FromResource("Bar/chair", Feet);
    public static Sprite Bottle => bottle != null ? bottle : bottle = FromResource("Bar/bottle", Feet);
    public static Sprite Stool => stool != null ? stool : stool = FromResource("Bar/stool", Feet);
    public static Sprite Table => table != null ? table : table = FromResource("Bar/table", Feet);
    public static Sprite TableFlipped => tableFlipped != null ? tableFlipped : tableFlipped = FromResource("Bar/table_flipped", Feet);
    // 2x2 white chip, tinted per shard
    public static Sprite Shard => shard != null ? shard : shard = Paint(2, 2, (x, y) => C(0xffffff), Center);

    public static Sprite Shadow => shadow != null ? shadow : shadow = FromResource("shadow");

    // pivot at the handle end; flip by negative x scale
    public static Sprite Whip => whip != null ? whip : whip = Paint(44, 3, (x, y) =>
        x >= 40 ? C(0xe0c080) : y == 1 ? C(0x6a3a1c) : Clear, new Vector2(0f, 0.5f));

}
