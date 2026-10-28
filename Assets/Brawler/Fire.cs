using System.Collections.Generic;
using UnityEngine;

namespace Brawler
{
    // a molotov's burning spirit on the floor: sets alight whoever stands in it, dies down after a few seconds
    public class Fire : MonoBehaviour
    {
        static readonly List<Fire> all = new List<Fire>();
        Vector2 ground;
        int life;
        SpriteRenderer[] flames;

        public static int Count => all.Count;

        public static void Spawn(Vector2 ground)
        {
            var f = new GameObject("Fire").AddComponent<Fire>();
            f.ground = ground;
            f.life = Feel.FireFrames;
            f.transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
            f.flames = new SpriteRenderer[5];
            for (int i = 0; i < f.flames.Length; i++) // spread across, alternately a little in front and behind
            {
                var sr = new GameObject("Flame").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(f.transform, false);
                float depth = i % 2 == 0 ? -2f : 2f;
                sr.transform.localPosition = new Vector3((i - 2) * 7f, depth, 0f);
                sr.sprite = Art.Flame[i % 3];
                sr.sortingOrder = -Mathf.RoundToInt(ground.y + depth) * 2;
                f.flames[i] = sr;
            }
            all.Add(f);
            Sfx.Play("fire");
        }

        public static bool Under(Vector2 ground)
        {
            foreach (var f in all)
                if (Mathf.Abs(f.ground.x - ground.x) < Feel.FireHalfWidth && Mathf.Abs(f.ground.y - ground.y) < Feel.FireHalfDepth)
                    return true;
            return false;
        }

        // stepped by Game with the fight
        public static void StepAll()
        {
            for (int i = all.Count - 1; i >= 0; i--)
                if (--all[i].life <= 0) { Kill(all[i]); all.RemoveAt(i); }
        }

        public static void Clear()
        {
            foreach (var f in all) Kill(f);
            all.Clear();
        }

        static void Kill(Fire f)
        {
            if (f == null) return;
            if (Application.isPlaying) Destroy(f.gameObject); else DestroyImmediate(f.gameObject);
        }

        // flicker, each flame out of step; shrinks away in its last second
        void LateUpdate()
        {
            float fade = Mathf.Clamp01(life / 60f);
            for (int i = 0; i < flames.Length; i++)
            {
                int k = Game.Frame / 4 + i;
                flames[i].sprite = Art.Flame[k % 3];
                flames[i].transform.localScale = new Vector3(1f, fade * (0.8f + 0.2f * (k * 7 % 3)), 1f);
            }
        }
    }
}
