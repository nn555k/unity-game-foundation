using System.Security.Cryptography;
using System.Text;

namespace GameFoundation.Save
{
    public static class SaveChecksum
    {
        /// <summary>
        /// 为版本号与正文生成稳定校验值，用于识别截断或损坏的存档。
        /// </summary>
        public static string Compute(int version, string payload)
        {
            var source = version + "\n" + (payload ?? string.Empty);
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(source));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var index = 0; index < bytes.Length; index++)
                {
                    builder.Append(bytes[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// 使用固定时间比较校验值，避免异常存档走入反序列化流程。
        /// </summary>
        public static bool IsValid(int version, string payload, string checksum)
        {
            if (string.IsNullOrEmpty(checksum))
            {
                return false;
            }

            var expected = Compute(version, payload);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(checksum);
            return expectedBytes.Length == actualBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
    }
}
