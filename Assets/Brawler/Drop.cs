using System.Collections.Generic;
using UnityEngine;

namespace Brawler
{
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
}
