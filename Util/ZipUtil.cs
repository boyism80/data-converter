using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace ExcelTableConverter.Util
{
    public static class ZipUtil
    {
        public static byte[] Zip(this byte[] buffer)
        {
            byte[] compressedByte;
            using (var ms = new MemoryStream())
            {
                using (var ds = new GZipStream(ms, CompressionMode.Compress))
                {
                    ds.Write(buffer, 0, buffer.Length);
                }
                compressedByte = ms.ToArray();
            }

            return compressedByte;
        }

        public static byte[] Zip<T>(this T obj)
        {
            var serialized = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(serialized);
            return bytes.Zip();
        }

        public static byte[] Unzip(this byte[] compressed)
        {
            using (var ms = new MemoryStream(compressed))
            {
                using (var gs = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var reader = new MemoryStream())
                    {
                        gs.CopyTo(reader);
                        return reader.ToArray();
                    }
                }
            }
        }

        public static object Unzip(this byte[] bytes, System.Type t)
        {
            var str = Encoding.UTF8.GetString(bytes.Unzip());
            return JsonConvert.DeserializeObject(str, t);
        }

        public static T Unzip<T>(this byte[] bytes)
        {
            return (T)bytes.Unzip(typeof(T));
        }
    }
}
