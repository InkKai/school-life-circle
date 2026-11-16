using System;
using System.Security.Cryptography;
using System.Text;

namespace Smile.Infrastructure
{
    /// <summary>密碼雜湊（PBKDF2-SHA256）與隨機碼產生</summary>
    public static class Security
    {
        private const int Iterations = 100000;

        public static void HashPassword(string password, out string hash, out string salt)
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(saltBytes);
            salt = Convert.ToBase64String(saltBytes);
            hash = Derive(password, saltBytes);
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false;
            var computed = Derive(password, Convert.FromBase64String(salt));
            return SlowEquals(computed, hash);
        }

        private static string Derive(string password, byte[] salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }

        private static bool SlowEquals(string a, string b)
        {
            var x = Encoding.ASCII.GetBytes(a);
            var y = Encoding.ASCII.GetBytes(b);
            int diff = x.Length ^ y.Length;
            for (int i = 0; i < x.Length && i < y.Length; i++) diff |= x[i] ^ y[i];
            return diff == 0;
        }

        // 去掉容易看錯的 0/O、1/I/L
        private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static string NewCouponCode()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var sb = new StringBuilder("SMILE-");
            foreach (var b in bytes) sb.Append(CodeChars[b % CodeChars.Length]);
            return sb.ToString();
        }

        /// <summary>10 位數字會員編號（店家可用數字鍵盤輸入）</summary>
        public static string NewMemberCode()
        {
            var bytes = new byte[10];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append((char)('0' + b % 10));
            if (sb[0] == '0') sb[0] = '8';
            return sb.ToString();
        }

        /// <summary>手機條碼載具格式：斜線開頭加 7 碼（數字、大寫英文、+ - .）</summary>
        public static bool IsValidCarrier(string code)
        {
            return code != null && System.Text.RegularExpressions.Regex.IsMatch(code, @"^/[0-9A-Z+\-.]{7}$");
        }

        /// <summary>8 碼邀請碼，放在邀請連結裡（跟集點用的會員編號分開）</summary>
        public static string NewInviteCode()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(CodeChars[b % CodeChars.Length]);
            return sb.ToString();
        }

        public static string NewShareToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public static class Geo
    {
        /// <summary>兩點距離（公尺），Haversine 公式</summary>
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000;
            double dLat = ToRad(lat2 - lat1), dLng = ToRad(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        public static string Format(double meters)
        {
            return meters < 1000 ? string.Format("{0:0} 公尺", meters) : string.Format("{0:0.0} 公里", meters / 1000);
        }

        /// <summary>步行時間（分鐘），以每分鐘 70 公尺估算</summary>
        public static int WalkMinutes(double meters)
        {
            return Math.Max(1, (int)Math.Ceiling(meters / 70));
        }

        private static double ToRad(double d) { return d * Math.PI / 180; }
    }
}
