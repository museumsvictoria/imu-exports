using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CommandLine;
using ImuExports.Infrastructure;
using ImuExports.Utilities;
using Serilog;

namespace ImuExports.Tasks.InsideOut
{
    public class InsideOutOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for json and images.", Required = true)]
        public string Destination { get; set; }

        [Option('a', "domain", HelpText = "Remote computer domain (not required)", Required = false)]
        public string Domain { get; set; }

        [Option('c', "computer", HelpText = "Remote computer (not required)", Required = false)]
        public string Computer { get; set; }

        [Option('u', "user", HelpText = "Remote computer user (not required)", Required = false)]
        public string User { get; set; }

        [Option('p', "password", HelpText = "Remote computer password (not required)", Required = false)]
        public string Password { get; set; }

        public Type TypeOfTask => typeof(InsideOutTask);

        public void Initialize()
        {
            // Add backslash if it doesnt exist to our destination directory
            if (!this.Destination.EndsWith(@"\"))
            {
                this.Destination += @"\";
            }

            var networkShareOptions = new[] { this.Domain, this.Computer, this.User, this.Password };
            NetworkShareAccesser networkShareAccesser = null;

            if (networkShareOptions.All(x => !string.IsNullOrEmpty(x)))
            {
                networkShareAccesser = NetworkShareAccesser.Access(this.Computer, this.Domain, this.User, this.Password);
            }
            else if(networkShareOptions.Any(x => !string.IsNullOrEmpty(x)))
            {
                Log.Logger.Error("Must supply all network share access options in order for it to work");
                Environment.Exit(Parser.DefaultExitCodeFail);
            }

            try
            {
                // Make sure destination directory exists
                if (!Directory.Exists(this.Destination))
                {
                    Directory.CreateDirectory(this.Destination);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Error creating {Destination} directory", this.Destination);
                Environment.Exit(Parser.DefaultExitCodeFail);
            }
            finally
            {
                networkShareAccesser?.Dispose();
            }
        }
    }
}