using System;

namespace ImuExports.NetFramework472.Tasks.InsideOut.Config
{
    public static class InsideOutConstants
    {
        private static readonly double ImagePadding = 0.9;

        public static readonly int MaxImageWidth = (int) Math.Round(1920 * ImagePadding);

        public static readonly int MaxImageHeight = (int) Math.Round(1080 * ImagePadding);
    }
}
