using System.Collections.Generic;
using UnityEngine;

namespace Brawler
{
    // Sound effects from Resources/Sfx (made by Tools/make_sfx.py; drop in a recording with the same name to replace one).
    public static class Sfx
    {
        static AudioSource source;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        // M toggles; remembered between sessions
        public static bool Muted
        {
            get => PlayerPrefs.GetInt("muted", 0) == 1;
            set { PlayerPrefs.SetInt("muted", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Play(string name, float volume = 1f)
        {
            if (!Application.isPlaying || Muted) return; // edit-mode tests run silent
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
