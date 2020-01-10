using System;

namespace DnDGen.Stress
{
    public class Logger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }
}
