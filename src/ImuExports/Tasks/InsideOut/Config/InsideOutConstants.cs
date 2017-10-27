namespace ImuExports.Tasks.InsideOut.Config
{
    public static class InsideOutConstants
    {
        private static readonly int ImagePadding = 72;

        public static readonly int MaxImageWidth = 1920 - (ImagePadding * 2);

        public static readonly int MaxImageHeight = 1080 - (ImagePadding * 2);
    }
}
