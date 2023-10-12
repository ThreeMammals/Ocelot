using System.Security.Cryptography;

namespace Ocelot.Cache
{
    public static class MD5Helper
    {
        public static string GenerateMd5(byte[] contentBytes)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(contentBytes);
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static string GenerateMd5(string contentString)
        {
            var contentBytes = Encoding.Unicode.GetBytes(contentString);
            return GenerateMd5(contentBytes);
        }
    }
}
