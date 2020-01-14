using System.Linq;

namespace Nienow.Helper
{
    public static class Utilities
    {
        public static bool AllNullOrEmpty(params string[] value)
        {
            return value.All(string.IsNullOrEmpty);
        }

        public static bool AllNullOrWhiteSpace(params string[] value)
        {
            return value.All(string.IsNullOrWhiteSpace);
        }
        
        public static bool AnyNullOrEmpty(params string[] value)
        {
            return value.Any(string.IsNullOrEmpty);
        }

        public static bool AnyNullOrWhiteSpace(params string[] value)
        {
            return value.Any(string.IsNullOrWhiteSpace);
        }
    }
}
