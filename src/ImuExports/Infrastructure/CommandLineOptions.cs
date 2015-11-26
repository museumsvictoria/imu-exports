using System;
using CommandLine;
using CommandLine.Text;

namespace ImuExports.Infrastructure
{
    public class CommandLineOptions
    {
        [Option('d', "dest", HelpText = "Destination directory", Required = true)]
        public string Destination { get; set; }

        [Option('a', "modified-after", HelpText = "Get all records after modified date >=")]
        public string ModifiedAfterDate { get; set; }

        [Option('b', "modified-before", HelpText = "Get all records before modified date <=")]
        public string ModifiedBeforeDate { get; set; }
        
        [HelpOption(HelpText = "Display this help screen.")]
        public string GetUsage()
        {
            var help = new HelpText("Imu Exports");

            var errors = help.RenderParsingErrorsText(this, 0);
            if (!string.IsNullOrEmpty(errors))
            {
                help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                help.AddPreOptionsLine(string.Concat(errors, Environment.NewLine));
            }

            help.AddPreOptionsLine("Usage: ImuExports.exe [-d|--dest destination] [-a|--modified-after 2015-01-23] [-b|--modified-before 2015-01-23] [-h|--help]");
            help.AddOptions(this);

            return help;
        }
    }
}