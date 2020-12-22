using System;
using System.IO;
using ImuExports.Tests.Resources;

namespace ImuExports.Tests
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