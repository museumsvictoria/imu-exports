using System;
using System.IO;
using Serilog;

namespace ALAExport.Export.Infrastructure
{
    public static class CommandLineConfig
    {
        public static CommandLineOptions Options;

        public static void Initialize(string[] args)
        {
            Options = new CommandLineOptions();

            if (!CommandLine.Parser.Default.ParseArguments(args, Options))
            {
                Environment.Exit(1);
            }

            // Add backslash if it doesnt exist to our destination directory
            if (!Options.Destination.EndsWith(@"\"))
            {
                Options.Destination += @"\";
            }
            // Make sure destination directory exists
            if (!Directory.Exists(Options.Destination))
            {
                try
                {
                    Directory.CreateDirectory(Options.Destination);
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error creating {Destination} directory", Options.Destination);
                    Environment.Exit(1);
                }
            }
        }
    }
}
