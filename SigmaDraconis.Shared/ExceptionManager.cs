namespace SigmaDraconis.Shared
{
    using System;
    using System.Collections.Generic;

    public static class ExceptionManager
    {
        public static List<Exception> CurrentExceptions = new List<Exception>();

        public static string DebugMessage { get; set; } = string.Empty;
    }
}
