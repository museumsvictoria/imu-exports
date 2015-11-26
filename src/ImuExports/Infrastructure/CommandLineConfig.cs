using System;

namespace ImuExports.Infrastructure
{
    public static class CommandLineConfig
    {
        public static CommandLineOptions Options;
        public static string InvokedVerb;
        public static object InvokedVerbInstance;

        public static void Initialize(string[] args)
        {
            Options = new CommandLineOptions();

            if (!CommandLine.Parser.Default.ParseArguments(args, Options,
                (verb, subOptions) =>
                {
                    InvokedVerb = verb;
                    InvokedVerbInstance = subOptions;
                }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var verbInstance = InvokedVerbInstance as IInitializable;

            if(verbInstance != null)
                verbInstance.Initialize();
        }
    }
}
