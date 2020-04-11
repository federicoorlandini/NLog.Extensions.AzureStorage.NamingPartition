using NLog;

namespace TestingApp
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Logger.Info("This is the test #1");
            Logger.Info("This is the test #2");
        }
    }
}
