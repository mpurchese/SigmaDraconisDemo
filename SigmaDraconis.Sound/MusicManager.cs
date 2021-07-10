namespace SigmaDraconis.Sound
{
    using Microsoft.Xna.Framework.Media;
    using System;
    using System.Collections.Generic;
    using Draconis.UI;
    using Settings;
    using Shared;

    public static class MusicManager
    {
        public static bool IsPlaying;

        private static readonly List<Song> songs = new List<Song>();
        private static readonly Queue<int> recentSongs = new Queue<int>();
        private static Random rand;
        private static bool isFadingOutMenuSong;
        private static bool isStarted;
        private static bool isErrored;

        public static void LoadContent()
        {
            isErrored = true;       // Disabled in this version
            return;

            try
            {
                songs.Add(UIStatics.Content.Load<Song>("Music/be-here-and-now"));
                songs.Add(UIStatics.Content.Load<Song>("Music/fabulous-journey"));
                songs.Add(UIStatics.Content.Load<Song>("Music/faraway-galaxies"));
                songs.Add(UIStatics.Content.Load<Song>("Music/flying-technology"));
                songs.Add(UIStatics.Content.Load<Song>("Music/journey-to-freedom"));
                songs.Add(UIStatics.Content.Load<Song>("Music/minimalist-dreams"));
                songs.Add(UIStatics.Content.Load<Song>("Music/seven-wonders"));
                songs.Add(UIStatics.Content.Load<Song>("Music/the-sky-at-night"));
                songs.Add(UIStatics.Content.Load<Song>("Music/xeno-tranquility"));
                rand = new Random();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("MusicManager", "Error in MusicManager.LoadContent(): " + ex.StackTrace);
            }
        }

        public static void Start()
        {
            isErrored = true;       // Disabled in this version
            return;

            recentSongs.Clear();
            IsPlaying = false;

            try
            {
                if (MediaPlayer.State == MediaState.Playing && MediaPlayer.Volume > 0.005f)
                {
                    isFadingOutMenuSong = true;
                    return;
                }

                MediaPlayer.Volume = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.MusicVolume) ?? 40) / 200f;
                var index = rand.Next(songs.Count);
                MediaPlayer.Play(songs[index]);
                recentSongs.Enqueue(index);
                IsPlaying = true;
                isStarted = true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("MusicManager", "Error in MusicManager.Start(): " + ex.StackTrace);
                isErrored = true;
            }
        }

        public static void Update()
        {
            if (isErrored) return;
            if (!isStarted && !isFadingOutMenuSong)
            {
                Start();
                return;
            }

            try
            {
                if (isFadingOutMenuSong)
                {
                    if (MediaPlayer.Volume > 0.005f) MediaPlayer.Volume -= 0.005f;
                    else
                    {
                        MediaPlayer.Stop();
                        isFadingOutMenuSong = false;
                        Start();
                    }

                    return;
                }

                var volume = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.MusicVolume) ?? 40) / 200f;
                if (MediaPlayer.Volume != volume) MediaPlayer.Volume = volume;
                if (IsPlaying && MediaPlayer.State == MediaState.Stopped && volume > 0f)
                {
                    var index = rand.Next(songs.Count);
                    while (recentSongs.Contains(index)) index = rand.Next(songs.Count);
                    MediaPlayer.Play(songs[index]);
                    recentSongs.Enqueue(index);
                    if (recentSongs.Count > 2) recentSongs.Dequeue();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("MusicManager", "Error in MusicManager.Update(): " + ex.StackTrace);
                isErrored = true;
            }
        }

        public static void Stop()
        {
            try
            {
                recentSongs.Clear();
                MediaPlayer.Stop();
                IsPlaying = false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("MusicManager", "Error in MusicManager.Stop(): " + ex.StackTrace);
                isErrored = true;
            }
        }
    }
}
