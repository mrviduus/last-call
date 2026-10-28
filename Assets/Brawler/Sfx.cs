using System.Collections.Generic;
using UnityEngine;

namespace Brawler
{
    // Sound effects from Resources/Sfx (made by Tools/make_sfx.py; drop in a recording with the same name to replace one).
    public static class Sfx
    {
        static AudioSource source;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Play(string name, float volume = 1f)
        {
            if (!Application.isPlaying) return; // edit-mode tests run silent
            if (source == null)
            {
                var go = new GameObject("Sfx");
                source = go.AddComponent<AudioSource>();
                if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
            }
            if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("Sfx/" + name);
            if (clip != null) source.PlayOneShot(clip, volume);
        }
    }
}
