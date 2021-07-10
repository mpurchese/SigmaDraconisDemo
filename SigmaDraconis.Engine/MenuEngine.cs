namespace SigmaDraconis.Engine
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Input;
    using UI;
    using Shared;
    using Settings;
    using Microsoft.Xna.Framework.Media;

    public class MenuEngine : IDisposable
    {
        public MenuScreen Screen { get { return this.MenuScreen; } }

        public static MenuEngine Instance { get; private set; }
        public MenuScreen MenuScreen { get; set; }
        //private Song menuSong;

        public event EventHandler<NewGameEventArgs> NewGameClick;
        public event EventHandler<LoadGameEventArgs> LoadGameClick;

        public MenuEngine(MenuScreen screen)
        {
            if (Instance == null)
            {
                Instance = this;

                this.MenuScreen = screen;
            }
            else
            {
                throw new ApplicationException("MenuEngine already created");
            }
        }

        public void LoadContent()
        {
            this.MenuScreen.LoadContent();
            this.MenuScreen.NewGameRequest += this.OnNewGameClick;
            this.MenuScreen.LoadGameRequest += this.OnLoadGameClick;
            //this.menuSong = UIStatics.Content.Load<Song>("Music/deep-space");
        }

        public void Update()
        {
            KeyboardManager.Update();
            MouseManager.Update();
            this.MenuScreen.Update();
            var volume = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.MusicVolume) ?? 40) / 200f;
            //if (MediaPlayer.State != MediaState.Playing && volume > 0f) MediaPlayer.Play(this.menuSong);
            MediaPlayer.Volume = volume;
        }

        public void Draw(GameTime gameTime)
        {
            this.MenuScreen.Draw();
        }

        private void OnNewGameClick(object sender, NewGameEventArgs e)
        {
            this.NewGameClick?.Invoke(this, e);
        }

        private void OnLoadGameClick(object sender, LoadGameEventArgs e)
        {
            this.LoadGameClick?.Invoke(this, e);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.MenuScreen != null) this.MenuScreen.Dispose();
            }
        }
    }
}
