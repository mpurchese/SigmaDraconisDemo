namespace SigmaDraconis.Shared
{
    using System;
    using System.Reflection;

    [Serializable]
    public class GameVersion
    {
        public static GameVersion CurrentGameVersion
        {
            get
            {
                var version = Assembly.GetEntryAssembly().GetName().Version;
                return new GameVersion(version.Major, version.Minor, version.Build);
            }
        }

        public GameVersion(int major, int minor, int build)
        {
            this.Major = major;
            this.Minor = minor;
            this.Build = build;
        }

        public bool IsCompatible => this.Minor >= 2 || this.Major > 0;

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }

        public override string ToString()
        {
            return $"{this.Major}.{this.Minor}.{this.Build}";
        }
    }
}
