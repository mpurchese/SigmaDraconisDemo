namespace SigmaDraconis.Shared
{
    using System;
    using System.IO;
    using System.Timers;

    public sealed class Logger : IDisposable
    {
        private StreamWriter sr;

        private string prevMsg;
        private readonly string path;
        private bool createdFile;
        private bool pendingFlush;
        private readonly Timer flushTimer;

        public static Logger Instance { get; private set; }

        public Logger()
        {
            if (Instance == null)
            {
                Instance = this;

                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                dir = Path.Combine(dir, "Logs");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                path = Path.Combine(dir, $"log_{DateTime.Now:yyyyMMddHHmmss}.txt");

                this.flushTimer = new Timer(1000) { AutoReset = false };
                this.flushTimer.Elapsed += this.OnFlushTimer;
            }
            else
            {
                throw new ApplicationException("Logger already created");
            }
        }

        private void OnFlushTimer(object sender, ElapsedEventArgs e)
        {
            if (this.pendingFlush) this.Flush();
        }

        public void Log(string sender, string message)
        {
            try
            {
                if (!this.createdFile)
                {
                    this.createdFile = true;
                    this.sr = File.CreateText(this.path);
                }

                if (message == this.prevMsg) return;  // Don't repeat identical messages
                else
                {
                    sr.WriteLine($"{DateTime.Now:HH:mm:ss} - {sender}: {message}");
                    this.pendingFlush = true;
                    this.flushTimer.Stop();
                    this.flushTimer.Start();
                }
            }
            catch
            {
                // Absorb exceptions on logging
            }

            this.prevMsg = message;
        }

        public void Flush()
        {
            try
            {
                this.flushTimer.Stop();
                if (sr != null) sr.Flush();
                this.pendingFlush = false;
            }
            catch
            {
                // Absorb exceptions on logging
            }
        }

        public void Dispose()
        {
            if (sr != null)
            {
                sr.Flush();
                sr.Close();
                sr.Dispose();
            }

            this.flushTimer.Dispose();
        }
    }
}
