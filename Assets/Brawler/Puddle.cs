using System.Collections.Generic;
using UnityEngine;

namespace Brawler
{
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
}
