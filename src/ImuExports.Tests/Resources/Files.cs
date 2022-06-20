using System.Reflection;

namespace ImuExports.Tests.Resources
{
    public static class Files
    {
        private static string RootFolder => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\Resources\Images\"));

        public static string OutputFolder => @$"{AppContext.BaseDirectory}Output\";
        
        public static string Crab => RootFolder + "cyclograpsus-granulosus.tif";
        
        public static string Fish => RootFolder + "parma-victoriae.tif";
        
        public static string Beetle => RootFolder + "metriorrhynchus-dilatipes.tif";
    }
    
}