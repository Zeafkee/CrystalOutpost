using System;
using UnityEngine;

namespace CrystalOutpost.Audio
{
    public static class GameSettings
    {
        private const string SoundKey = "sound_on";
        private const string MusicKey = "music_on";

        public static event Action Changed;

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => Set(SoundKey, value);
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => Set(MusicKey, value);
        }

        private static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
