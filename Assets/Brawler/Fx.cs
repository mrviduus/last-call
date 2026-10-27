using System.Collections.Generic;
using UnityEngine;

// Short-lived pictures over the fight: hit sparks, dust puffs, afterimages. Cosmetic only.
// They count real fixed ticks, so a spark keeps animating while hitstop freezes the fight.
public class Fx : MonoBehaviour
{
    Sprite[] frames;
    int age, frameTime;
    float fade; // afterimages: starting alpha, fades to 0; 0 = no fade
    SpriteRenderer sr;

    static Fx Make(Sprite[] frames, Vector2 at, int frameTime, int order)
    {
        if (!Application.isPlaying) return null; // edit-mode tests: nothing to see
        var sr = new GameObject("Fx").AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = order;
        sr.transform.position = new Vector3(Mathf.Round(at.x), Mathf.Round(at.y), 0f);
        var fx = sr.gameObject.AddComponent<Fx>();
        fx.frames = frames;
        fx.frameTime = frameTime;
        fx.sr = sr;
        return fx;
    }

    // a star where the blow lands, in front of everyone; heavy blows get a bigger one
    public static void Spark(Vector2 at, bool big)
    {
        var fx = Make(Art.Spark, at, 2, 25000);
        if (fx != null && big) fx.transform.localScale = new Vector3(2f, 2f, 1f);
    }

    // a puff on the floor at someone's feet (landing, running, hitting the ground)
    public static void Dust(Vector2 ground) => Make(Art.Dust, ground, 4, -Mathf.RoundToInt(ground.y) * 2 + 1);

    // blood: drops fly from a point above the floor (dir: -1 left, 1 right, 0 all round), fall back to it and
    // stay there as stains; force > 3 is a burst, a high force with dir 0 a fountain
    public static void Blood(Vector2 ground, float height, int count, float dir, float force)
    {
        if (!Application.isPlaying) return;
        for (int i = 0; i < count; i++)
            Drop.Spawn(ground, height, (dir == 0f ? Random.Range(-1f, 1f) : dir * Random.Range(0.3f, 1f)) * force * 0.6f
                + Random.Range(-0.5f, 0.5f), Random.Range(1f, 2.5f) + force * 0.5f * Random.Range(0.5f, 1.2f));
    }

    // a fading blue copy of a body: fast moves leave a trail
    public static void Ghost(SpriteRenderer body)
    {
        var fx = Make(new[] { body.sprite }, body.transform.position, Feel.GhostFrames, body.sortingOrder - 1);
        if (fx == null) return;
        fx.sr.flipX = body.flipX;
        fx.sr.sharedMaterial = Art.Silhouette;
        fx.fade = 0.5f;
        fx.sr.color = new Color(0.55f, 0.8f, 1f, fx.fade);
    }

    void FixedUpdate()
    {
        age++;
        int i = age / frameTime;
        if (i >= frames.Length) { Destroy(gameObject); return; }
        sr.sprite = frames[i];
        if (fade > 0f)
        {
            var c = sr.color;
            c.a = fade * (1f - (float)age / (frames.Length * frameTime));
            sr.color = c;
        }
    }
}

// one drop of blood in flight; lands as a stain on the floor, where it stays (the oldest go past MaxStains)
public class Drop : MonoBehaviour
{
    const int MaxStains = 500;
    static readonly Queue<GameObject> stains = new Queue<GameObject>();
    static readonly Color32[] Reds = { new Color32(0xc0, 0x10, 0x14, 255), new Color32(0x90, 0x08, 0x0c, 255), new Color32(0xe0, 0x28, 0x20, 255) };

    Vector2 ground;
    float height, vx, vh;

    public static void Spawn(Vector2 ground, float height, float vx, float vh)
    {
        var sr = new GameObject("Blood").AddComponent<SpriteRenderer>();
        sr.sprite = Art.Shard;
        sr.color = Reds[Random.Range(0, Reds.Length)];
        sr.sortingOrder = -Mathf.RoundToInt(ground.y) * 2 + 1;
        var d = sr.gameObject.AddComponent<Drop>();
        d.ground = ground;
        d.height = height;
        d.vx = vx;
        d.vh = vh;
        if (Random.value < 0.4f) d.transform.localScale = new Vector3(2f, 2f, 1f); // some fat drops, readable on a big screen
        d.transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y + height), 0f);
    }

    // a new fight: the floor is mopped
    public static void Clear()
    {
        foreach (var d in FindObjectsByType<Drop>(FindObjectsSortMode.None)) Destroy(d.gameObject);
        while (stains.Count > 0) Destroy(stains.Dequeue());
    }

    void FixedUpdate()
    {
        vh -= Feel.Gravity;
        height += vh;
        ground.x += vx;
        if (height > 0f)
        {
            transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y + height), 0f);
            return;
        }
        // splat: a dark smear on the floor, wider the faster it fell
        var sr = GetComponent<SpriteRenderer>();
        sr.color = Reds[1];
        sr.sortingOrder = -19000; // on the floor, under everyone and everything
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
        transform.localScale = new Vector3((vh < -4f ? 3f : vh < -2f ? 2f : 1f) * transform.localScale.x, transform.localScale.y, 1f);
        stains.Enqueue(gameObject);
        if (stains.Count > MaxStains) Destroy(stains.Dequeue());
        Destroy(this);
    }
}

// a spilled drink on the floor, from a bottle that broke: running, landing or being knocked across it is a slip
public class Puddle : MonoBehaviour
{
    static readonly List<Puddle> all = new List<Puddle>();
    Vector2 ground;
    int life;

    public static void Spawn(Vector2 ground)
    {
        var sr = new GameObject("Puddle").AddComponent<SpriteRenderer>();
        sr.sprite = Art.Puddle;
        sr.sortingOrder = -18999; // on the floor, over the blood stains
        sr.transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
        var p = sr.gameObject.AddComponent<Puddle>();
        p.ground = ground;
        p.life = Feel.PuddleFrames;
        all.Add(p);
    }

    public static bool Under(Vector2 ground)
    {
        foreach (var p in all)
            if (Mathf.Abs(p.ground.x - ground.x) < Feel.PuddleHalfWidth && Mathf.Abs(p.ground.y - ground.y) < Feel.PuddleHalfDepth)
                return true;
        return false;
    }

    // stepped by Game with the fight, so hitstop doesn't dry them
    public static void StepAll()
    {
        for (int i = all.Count - 1; i >= 0; i--)
            if (--all[i].life <= 0) { Kill(all[i]); all.RemoveAt(i); }
    }

    public static void Clear()
    {
        foreach (var p in all) Kill(p);
        all.Clear();
    }

    static void Kill(Puddle p)
    {
        if (p == null) return;
        if (Application.isPlaying) Destroy(p.gameObject); else DestroyImmediate(p.gameObject);
    }
}
