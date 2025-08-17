using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GiniAccountManagement
{
    public class AppConfig
    {
        public static AppConfig Current { get; private set; } = new();
        public string ProfileFilePath { get; set; } = string.Empty;
        public string ProfilesRootDir { get; set; } = string.Empty;
        public bool RequireVpn { get; set; }
        public static AppConfig Load()
        {
            var configPath = GetConfigPath();
            string baseDir = AppContext.BaseDirectory;

            AppConfig config;

            if (!File.Exists(configPath))
            {
                // 1. Nếu chưa có file → tạo config mặc định
                config = CreateDefaultConfig();
                Save(config);
            }
            else
            {
                // 2. Nếu có file → cố gắng đọc
                try
                {
                    var rawJson = File.ReadAllText(configPath);
                    var cleanJson = StripJsonComments(rawJson);
                    config = JsonSerializer.Deserialize<AppConfig>(cleanJson) ?? CreateDefaultConfig();
                }
                catch
                {
                    // 3. Nếu lỗi khi đọc hoặc hỏng file → dùng config mặc định
                    config = CreateDefaultConfig();
                    Save(config);
                }
            }

            // 4. Normalize đường dẫn nếu là tương đối
            config.ProfileFilePath = NormalizePath(config.ProfileFilePath, baseDir);
            config.ProfilesRootDir = NormalizePath(config.ProfilesRootDir, baseDir);

            Current = config;
            return config;
        }

        private static string StripJsonComments(string json)
        {
            var lines = json.Split('\n')
                            .Where(line => !line.TrimStart().StartsWith("//"))
                            .ToList();
            return string.Join("\n", lines);
        }
        private static AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                ProfileFilePath = Path.Combine(FileSystem.AppDataDirectory, "profiles.json"),
                ProfilesRootDir = Path.Combine(FileSystem.AppDataDirectory, "ChromeProfiles"),
                RequireVpn = true
            };
        }
        private static string NormalizePath(string path, string baseDir)
        {
            if (Path.IsPathRooted(path))
            {
                // Đường dẫn tuyệt đối → giữ nguyên
                return Path.GetFullPath(path);
            }
            // Đường dẫn tương đối → nối với baseDir
            return Path.GetFullPath(Path.Combine(baseDir, path));
        }
        public static void Save(AppConfig config)
        {
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
        }

        private static string GetConfigPath()
        {
            string baseDir = AppContext.BaseDirectory;
#if DEBUG
            // Nếu đang chạy trong bin/Debug/... → đi lên 2-3 cấp để tìm project root
            var dir = new DirectoryInfo(baseDir);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "config.json")))
            {
                dir = dir.Parent;
            }

            if (dir != null)
                return Path.Combine(dir.FullName, "config.json");
#endif
            // Mặc định: dùng baseDir
            return Path.Combine(baseDir, "config.json");
        }
    }
}
