using System;
using System.Collections.Generic;
using System.IO;

namespace Nienow.Helper
{
    public static class ExtensionMethods
    {
        /// <summary>Aplica ToLower e remove acentos (mesma tabela do original).</summary>
        public static string CleanText(this string value) =>
            value.ToLower()
                .Replace('á', 'a').Replace('à', 'a').Replace('â', 'a').Replace('ã', 'a').Replace('ā', 'a')
                .Replace('é', 'e').Replace('è', 'e').Replace('ê', 'e').Replace('ē', 'e')
                .Replace('í', 'i').Replace('ì', 'i').Replace('î', 'i').Replace('ī', 'i')
                .Replace('ó', 'o').Replace('ò', 'o').Replace('ô', 'o').Replace('õ', 'o').Replace('ō', 'o')
                .Replace('ú', 'u').Replace('ù', 'u').Replace('û', 'u').Replace('ū', 'u');

        /// <summary>Fisher–Yates in-place (mesmo algoritmo do original, com <see cref="Random.Shared"/>).</summary>
        public static IList<T> ShuffleList<T>(this IList<T> list, Random? rng = null)
        {
            rng ??= Random.Shared;
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
            return list;
        }

        public static byte[] ToByteArray(this Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
