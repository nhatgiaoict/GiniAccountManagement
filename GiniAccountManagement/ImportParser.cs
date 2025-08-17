using GiniAccountManagement.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GiniAccountManagement
{
   
    public static class ImportParser
    {
        private static readonly AppConfig _config = AppConfig.Current;
        public static List<Account> ParseAccounts(string text)
        {
            var result = new List<Account>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line);
                }
            }

            if (!lines.Any()) return result;

            var delim = DetectDelimiter(lines[0]);
            var hasHeader = LooksLikeHeader(lines[0]);
            var headers = hasHeader ? SplitRow(lines[0], delim) : Array.Empty<string>();
            int start = hasHeader ? 1 : 0;

            for (int i = start; i < lines.Count; i++)
            {
                var cols = SplitRow(lines[i], delim);
                var acc = hasHeader ? FromHeaderMap(headers, cols) : FromPositional(cols);
                acc.ProfilePath = $"{_config.ProfilesRootDir}/{acc.UID}";
                result.Add(acc);
            }

            return result;
        }

        private static char DetectDelimiter(string s)
        {
            if (s.Contains('\t')) return '\t';
            if (s.Contains('|')) return '|';
            return ','; // mặc định CSV
        }

        private static bool LooksLikeHeader(string s)
        {
            var t = s.ToLowerInvariant();
            return t.Contains("uid") || t.Contains("email") || t.Contains("token");
        }

        private static string[] SplitRow(string line, char d)
        {
            if (d == ',')
            {
                // CSV tôn trọng dấu "
                var list = new List<string>();
                var rx = new Regex("(?:^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");
                foreach (Match m in rx.Matches(line))
                {
                    var val = m.Value.TrimStart(',');
                    if (val.StartsWith('"') && val.EndsWith('"'))
                        val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
                    list.Add(val);
                }
                return list.ToArray();
            }
            return line.Split(d);
        }

        private static Account FromHeaderMap(string[] headers, string[] cols)
        {
            string Get(string name)
            {
                for (int i = 0; i < headers.Length; i++)
                    if (string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase))
                        return i < cols.Length ? cols[i]?.Trim() ?? "" : "";
                return "";
            }

            var a = new Account
            {
                UID = Get("uid"),
                Token = Get("token"),
                Email = Get("email"),
                Ten = Get("ten"),
                GioiTinh = Get("gioitinh"),
                Password = Get("password"),
                EmailRecovery = Get("email recovery"),
                PasswordEmailRecovery = Get("email recovery password"),
                TwoFA = Get("2fa"),
                Proxy = Get("proxy"),
                Cookie = Get("cookie"),
                Note = Get("note"),
                ThuMuc = Get("thumuc")
            };

            if (int.TryParse(Get("banbe"), out var bb)) a.BanBe = bb;
            if (int.TryParse(Get("nhom"), out var nh)) a.Nhom = nh;

            if (TryParseDate(Get("ngaysinh"), out var dob)) a.NgaySinh = dob.ToString();
            if (TryParseDate(Get("lantuongtaccuoi"), out var last)) a.LanTuongTacCuoi = last.ToString();

            return a;
        }

        private static Account FromPositional(string[] c)
        {
            // Thứ tự gợi ý:
            // 0 UID, 1 Email, 2 Ten, 3 BanBe, 4 Nhom, 5 NgaySinh, 6 GioiTinh,
            // 7 MatKhau, 8 EmailKhoiPhuc, 9 Ma2FA, 10 Proxy, 11 Profile, 12 ThuMuc, 13 LanTuongTacCuoi, 14 Token
            string at(int i) => i < c.Length ? c[i]?.Trim() ?? "" : "";

            var a = new Account
            {
                UID = at(0),
                Password = at(1),
                TwoFA = at(2),
                Email = at(3),
                EmailPassword = at(4),              
                EmailRecovery = at(5),
                PasswordEmailRecovery = at(6),
                Cookie = at(7),
                Token = at(8),
                Note = at(9)
            };

            if (int.TryParse(at(3), out var bb)) a.BanBe = bb;
            if (int.TryParse(at(4), out var nh)) a.Nhom = nh;

            if (TryParseDate(at(5), out var dob)) a.NgaySinh = dob.ToString();
            if (TryParseDate(at(13), out var last)) a.LanTuongTacCuoi = last.ToString();

            return a;
        }

        private static bool TryParseDate(string? s, out DateTime dt)
        {
            dt = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var fmts = new[] { "yyyy-MM-dd","dd/MM/yyyy","MM/dd/yyyy",
                           "yyyy-MM-dd HH:mm:ss","dd/MM/yyyy HH:mm:ss" };
            return DateTime.TryParseExact(s, fmts, CultureInfo.InvariantCulture,
                                          DateTimeStyles.AssumeLocal, out dt)
                   || DateTime.TryParse(s, out dt);
        }
    }
}
