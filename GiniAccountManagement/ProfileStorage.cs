using GiniAccountManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GiniAccountManagement
{
    public class ProfileStorage
    {
        private static readonly AppConfig currentConfig = AppConfig.Current;


        static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        static readonly SemaphoreSlim Gate = new(1, 1);

        public static async Task SaveAsync(IEnumerable<Account> accounts)
        {
            await Gate.WaitAsync();
            try
            {
                var dir = Path.GetDirectoryName(currentConfig.ProfileFilePath)!;
                Directory.CreateDirectory(dir);

                var newList = accounts.ToList();
                List<Account> finalList;

                // 1. Nếu file đã tồn tại → kiểm tra trùng nội dung
                if (File.Exists(currentConfig.ProfileFilePath))
                {
                    var oldJson = await File.ReadAllTextAsync(currentConfig.ProfileFilePath);
                    var oldAccounts = JsonSerializer.Deserialize<List<Account>>(oldJson, JsonOpts) ?? new();
                    // 2. Bỏ qua các account có UID đã tồn tại
                    var oldUids = new HashSet<string>(oldAccounts.Select(a => a.UID));
                    var filteredNew = newList.Where(a => !oldUids.Contains(a.UID)).ToList();

                    // Nếu không có gì mới → bỏ qua
                    if (filteredNew.Count == 0)
                        return;
                    // 3. Gộp danh sách
                    finalList = oldAccounts.Concat(filteredNew).ToList();
                    // 4. Backup file gốc
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupPath = Path.Combine(dir, $"profiles_{timestamp}.json.bak");
                    File.Copy(currentConfig.ProfileFilePath, backupPath, overwrite: true);
                }
                else
                {
                    finalList = newList;
                }
                // 5. Ghi file mới
                using var stream = File.Create(currentConfig.ProfileFilePath);
                await JsonSerializer.SerializeAsync(stream, finalList, JsonOpts);

                // 4. Xoá bớt backup cũ nếu quá 10 file
                var backupFiles = Directory.GetFiles(dir, "profiles_*.json.bak")
                                           .OrderByDescending(File.GetCreationTime)
                                           .Skip(10)
                                           .ToList();
                foreach (var oldFile in backupFiles)
                {
                    try { File.Delete(oldFile); } catch { /* bỏ qua lỗi xóa */ }
                }
            }
            finally { Gate.Release(); }
        }
        public static async Task<List<Account>> LoadAsync()
        {
            try
            {
                if (!File.Exists(currentConfig.ProfileFilePath)) return new List<Account>();
                await using var stream = File.OpenRead(currentConfig.ProfileFilePath);
                var list = await JsonSerializer.DeserializeAsync<List<Account>>(stream, JsonOpts)
                           ?? new List<Account>();
                return list;
            }
            catch
            {
                // Nếu file hỏng, trả list rỗng (hoặc bạn có thể ném exception tùy ý)
                return new List<Account>();
            }
        }
    }
}
