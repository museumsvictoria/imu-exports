using System.Linq;

namespace ImuExports.Extensions
{
    public static class ObjectExtensions
    {
        public static bool AllStringPropertiesNullOrEmpty(this object obj)
        {
            return obj.GetType().GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .Select(x => (string)x.GetValue(obj))
                .All(string.IsNullOrEmpty);
        }
    }
}
