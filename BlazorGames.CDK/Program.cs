using Amazon.CDK;

namespace BlazorGames.CDK
{
    public static class Constants
    {
        public static string AppName = "BlazorGames";
    }
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var env = MakeEnv();


            var networkStack = new NetworkStack(app, $"{Constants.AppName}Network", new StackProps { Env = env });
            new AppRunnerStack(app, $"{Constants.AppName}AppRunner", new AppRunnerStackProps {  Env = env, Vpc = networkStack.Vpc });            
            app.Synth();
            
        }

        private static Environment MakeEnv(string account = null, string region = null)
        {
            return new Environment
            {
                Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            };
        }        
    }
}
