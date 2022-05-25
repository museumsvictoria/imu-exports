using System.IO;
using System.Reflection;

namespace ImuExports.NetFramework472.Tests.Resources
{
    public static class Files
    {
        private static string RootFolder => @"..\..\ImuExports.NetFramework472.Tests\Resources\Images\";
        
        public static string OutputFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Output\";
        
        public static string Crab => RootFolder + @"cyclograpsus-granulosus.tif";
        
        public static string Fish => RootFolder + @"parma-victoriae.tif";
        
        public static string Beetle => RootFolder + @"metriorrhynchus-dilatipes.tif";
    }
}