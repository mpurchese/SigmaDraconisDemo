namespace SigmaDraconis.Sound
{
    using Microsoft.Xna.Framework.Audio;
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;

    internal class SoundInstance
    {
        private readonly SoundEffectInstance effect;
        private float volume;
        private float pan;
        private float pitch;

        private static bool isErrored;

        public Vector2f WorldPos { get; set; }

        public SoundInstance(Vector2f worldPos, string soundName, bool repeat)
        {
            if (isErrored) return;

            this.WorldPos = worldPos;

            try
            {
                var se = UIStatics.Content.Load<SoundEffect>("Sounds/" + soundName);
                this.effect = se.CreateInstance();
                this.effect.IsLooped = repeat;
                if (!repeat) this.effect.Play();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SoundInstance", "Error in SoundInstance ctor: " + ex.StackTrace);
                this.effect.Stop();
                isErrored = true;
            }
        }

        public void UpdateParams(float volume, float pan, float pitch)
        {
            this.volume = volume;
            this.pan = pan;
            this.pitch = pitch;
        }

        public void UpdateEffect(float globalVolume)
        {
            if (isErrored) return;
            if (!this.effect.IsLooped && this.effect.State == SoundState.Stopped) return;

            try
            {
                var vol = this.volume * globalVolume;
                if (vol < 0.001f && this.effect.State == SoundState.Playing)
                {
                    this.effect.Pause();
                }
                else if (vol >= 0.001f)
                {
                    if (this.effect.State == SoundState.Paused) this.effect.Resume();
                    else if (this.effect.State == SoundState.Stopped) this.effect.Play();

                    vol = vol.Clamp(0f, 1f);
                    if (this.effect.Volume != vol) this.effect.Volume = vol;
                    if (this.effect.Pan != pan) this.effect.Pan = this.pan;
                    if (this.effect.Pitch != pitch) this.effect.Pitch = this.pitch;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SoundInstance", "Error in SoundInstance.UpdateEffect(): " + ex.StackTrace);
                this.effect.Stop();
                isErrored = true;
            }
        }

        public void Stop()
        {
            if (isErrored) return;

            try
            {
                this.effect.Stop();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SoundInstance", "Error in SoundInstance.Stop(): " + ex.StackTrace);
                isErrored = true;
            }
        }
    }
}
