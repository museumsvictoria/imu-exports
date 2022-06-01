using CommandLine;
using ImuExports.Tasks.AtlasOfLivingAustralia;
using ImuExports.Tasks.AusGeochem;

namespace ImuExports.Configuration
{
    public static class CommandOptions
    {
        public static ITaskOptions? TaskOptions;

        public static void Initialize(string[] args)
        {
            Parser.Default
                .ParseArguments<AtlasOfLivingAustraliaOptions, AusGeochemOptions>(args)
                .WithParsed(options =>
                {
                    // Arguments parsed successfully so assign to global options
                    TaskOptions = (ITaskOptions)options;
                })
                .WithNotParsed(errors =>
                {
                    // Exit with error code, errors automatically output to cli
                    Environment.Exit(-1);
                });
        }
    }
}
