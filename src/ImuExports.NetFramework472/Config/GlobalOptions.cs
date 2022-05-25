using System;
using ImuExports.NetFramework472.Infrastructure;

namespace ImuExports.NetFramework472.Config
{
    public static class GlobalOptions
    {
        public static Options Options;
        public static object TaskOptions;

        public static void Initialize(string[] args)
        {
            Options = new Options();

            if (!CommandLine.Parser.Default.ParseArguments(args, Options,
                (verb, subOptions) =>
                {
                    TaskOptions = subOptions;
                }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var taskOptions = TaskOptions as ITaskOptions;

            taskOptions?.Initialize();
        }
    }
}
