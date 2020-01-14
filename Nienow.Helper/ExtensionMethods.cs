using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Nienow.Helper
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Apply the ToLower and removethe accents.
        /// </summary>
        public static string CleanText(this string value)
        {
            value = value.ToLower()
                .Replace('á', 'a')
                .Replace('à', 'a')
                .Replace('â', 'a')
                .Replace('ã', 'a')
                .Replace('ā', 'a')
                .Replace('é', 'e')
                .Replace('è', 'e')
                .Replace('ê', 'e')
                .Replace('ē', 'e')
                .Replace('í', 'i')
                .Replace('ì', 'i')
                .Replace('î', 'i')
                .Replace('ī', 'i')
                .Replace('ó', 'o')
                .Replace('ò', 'o')
                .Replace('ô', 'o')
                .Replace('õ', 'o')
                .Replace('ō', 'o')
                .Replace('ú', 'u')
                .Replace('ù', 'u')
                .Replace('û', 'u')
                .Replace('ū', 'u');

            return value;
        }

        /// <summary>
        /// Redefines a size proportionaly
        /// </summary>
        /// <param name="maxWidth">Max width</param>
        /// <param name="maxHeight">Max height</param>
        public static Size Resize(this Size size, int maxWidth, int maxHeight)
        {
            double w = (double)size.Width;
            double h = (double)size.Height;

            while (w > maxWidth || h > maxHeight)
            {
                w = w - w * 0.01;
                h = h - h * 0.01;
            }

            return new Size((int)w, (int)h);
        }

        public static IList<T> ShuffleList<T>(this IList<T> list)
        {
            Random rng = new Random((int)DateTime.Now.Ticks);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }

        public static byte[] ToByteArray(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);

                return ms.ToArray();
            }
        }
    }
}
