using System;
using System.Collections.Generic;

namespace Nienow.Helper
{
    public static class ExtensionMethods
    {
        /// <summary>Fisher–Yates in-place (mesmo algoritmo do original, com <see cref="Random.Shared"/>).</summary>
        public static IList<T> ShuffleList<T>(this IList<T> list, Random? rng = null)
        {
            rng ??= Random.Shared;
            for (var n = list.Count - 1; n > 0; n--)
            {
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
            return list;
        }
    }
}
