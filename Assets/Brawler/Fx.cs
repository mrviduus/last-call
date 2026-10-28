using UnityEngine;

namespace Brawler
{
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
}
