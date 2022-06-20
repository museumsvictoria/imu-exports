using System;
using System.IO;
using ImuExports.NetFramework472.Tests.Resources;

namespace ImuExports.NetFramework472.Tests
{
    public class FileBasedTest : IDisposable
    {
        protected FileBasedTest()
        {
            Directory.CreateDirectory(Files.OutputFolder);
        }

        public void Dispose()
        {
        }
    }
}