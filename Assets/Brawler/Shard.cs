using UnityEngine;

namespace Brawler
{
    // a bit of glass or wood flying off a broken prop; cosmetic only
    public class Shard : MonoBehaviour
    {
        Vector2 vel;
        int life = 40;

        public static void Spawn(Vector3 at, Color32 color)
        {
            var sr = new GameObject("Shard").AddComponent<SpriteRenderer>();
            sr.sprite = Art.Shard;
            sr.color = color;
            sr.sortingOrder = 20000;
            sr.transform.position = at;
            var s = sr.gameObject.AddComponent<Shard>();
            s.vel = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1.5f, 4f));
        }

        void FixedUpdate()
        {
            vel.y -= Feel.Gravity;
            transform.position += (Vector3)vel;
            if (--life <= 0) Destroy(gameObject);
        }
    }
}
