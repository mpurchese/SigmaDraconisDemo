using System;
using SigmaDraconis.AI;
using SigmaDraconis.Engine;
using SigmaDraconis.Shared;
using SigmaDraconis.WorldGenerator;

namespace SigmaDraconis
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (var game = new GameCoreDX())
                {
                    Logger.Instance.Log("Main", "Started");
                    game.Run();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Main", ex.ToString());
                Logger.Instance.Flush();
            }
        }
    }
#endif
}
