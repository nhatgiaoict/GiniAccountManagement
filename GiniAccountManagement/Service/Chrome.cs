using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiniAccountManagement.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GiniAccountManagement.Service
{
    public static class Chrome
    {
        // profileKey -> driver
        private static readonly ConcurrentDictionary<string, IWebDriver> _drivers = new();
        private static readonly SemaphoreSlim _gate = new(1, 1);
        public static IReadOnlyCollection<string> CurrentProfiles => _drivers.Keys.ToList();

        static Chrome()
        {
            // Fallback cleanup khi tiến trình thoát (mọi nền tảng)
            AppDomain.CurrentDomain.ProcessExit += (_, __) => CloseAllSafe();
            AppDomain.CurrentDomain.UnhandledException += (_, __) => CloseAllSafe();
        }
        // Mở (hoặc lấy lại) driver cho 1 profile
        public static async Task<IWebDriver> OpenOrGetAsync(Account acc)
        {
            await _gate.WaitAsync();
            try
            {

                // Nếu đã có & còn sống → dùng lại
                if (_drivers.TryGetValue(acc.UID, out var existing) && IsAlive(existing))
                    return existing;

                // Nếu có record nhưng driver chết → dọn
                if (existing is not null) TryQuit(existing);
                _drivers.TryRemove(acc.UID, out _);

                // Tạo driver mới
                var driver = await CreateChromeDriver(acc);
                _drivers[acc.UID] = driver;
                return driver;
            }
            finally
            {
                _gate.Release();
            }
        }
        public static async Task CloseAsync(Account acc)
        {
            await CloseAsync(acc.UID);
        }

        public static async Task CloseAsync(string profileKey)
        {
            await _gate.WaitAsync();
            try
            {
                if (_drivers.TryRemove(profileKey, out var drv))
                    TryQuit(drv);
            }
            finally { _gate.Release(); }
        }

        public static void CloseAllSafe()
        {
            try
            {
                foreach (var kv in _drivers.ToArray())
                    TryQuit(kv.Value);
                _drivers.Clear();
            }
            catch { /* ignore */ }
        }

        // ------- helpers -------
        private static bool IsAlive(IWebDriver d)
        {
            try
            {
                if (d is null) return false;
                var _ = ((IJavaScriptExecutor)d).ExecuteScript("return 1");
                return true;
            }
            catch { return false; }
        }

        private static void TryQuit(IWebDriver d)
        {
            try { d?.Quit(); } catch { }
            try { d?.Dispose(); } catch { }
        }

        public static async Task<IWebDriver> CreateChromeDriver(Account a, bool headless = false)
        {
            var url = "https://www.facebook.com";
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-intensive-wake-up-throttling");
            options.AddArgument("--disable-tab-groups-collapse-freezing");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument($"--user-data-dir={a.ProfilePath}");

            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-blink-features");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--allow-running-insecure-content");
            options.AddArgument("--fontrenderhinting[none]");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddLocalStatePreference("browser.enabled_labs_experiments", new string[]
            {
        "calculate-native-win-occlusion@2",
        "intensive-wake-up-throttling@3",
        "tab-groups-collapse-freezing@2"
            });
            try
            {
                options.AddLocalStatePreference("browser.last_whats_new_version", 10000);
            }
            catch (Exception ex)
            {
                // Handle exception if needed
            }

            if (!string.IsNullOrEmpty(a.UserAgent))
            {
                options.AddArgument("--user-agent=" + a.UserAgent);
            }
            else
            {
                string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                options.AddArgument($"--user-agent={userAgent}");
            }

            if (!string.IsNullOrEmpty(a.Proxy))
            {
                var parts = a.Proxy.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4)
                {
                    options.AddArgument($"--proxy-server=http://{parts[0]}:{parts[1]}");
                    // Tạo tiện ích mở rộng Proxy Auto-Auth
                    var proxyAuthExtensionPath = CreateProxyAuthExtension(a.Proxy, a.ProfilePath);

                    options.AddArgument($"--load-extension={proxyAuthExtensionPath}");
                }
                else
                {
                    options.AddArgument($"--proxy-server={a.Proxy}");
                }
            }

            var driver = new ChromeDriver(chromeDriverService, options);
            driver.Manage().Window.Minimize();
            driver.Navigate().GoToUrl(url);

            return await Task.FromResult(driver);
        }
        private static string CreateProxyAuthExtension(string proxy, string profilePath)
        {
            var parts = proxy.Split(':');
            if (parts.Length != 4)
                return "";

            var proxyHost = parts[0];
            var proxyPort = parts[1];
            var proxyUser = parts[2];
            var proxyPass = parts[3];

            // Tạo file manifest.json cho tiện ích mở rộng
            var manifestJson = @"
            {
    ""version"": ""1.0.0"",
    ""manifest_version"": 2,
    ""name"": ""Proxy Auto Auth"",
    ""permissions"": [
        ""<all_urls>"",
        ""webRequest"",
        ""webRequestBlocking"",
        ""proxy""
    ],
    ""background"": {
        ""scripts"": [""background.js""]
    }
}";

            // Tạo file background.js để thêm thông tin xác thực proxy
            var backgroundJs = $@"
            var config = {{
                mode: 'fixed_servers',
                rules: {{
                    singleProxy: {{
                        scheme: 'http',
                        host: '{proxyHost}',
                        port: parseInt({proxyPort})
                    }},
                    bypassList: ['localhost']
                }}
            }};

            chrome.proxy.settings.set({{value: config, scope: 'regular'}}, function() {{}});

            function callbackFn(details) {{
                return {{
                    authCredentials: {{
                        username: '{proxyUser}',
                        password: '{proxyPass}'
                    }}
                }};
            }}

            chrome.webRequest.onAuthRequired.addListener(
                callbackFn,
                {{urls: ['<all_urls>']}},
                ['blocking']
            );
            ";

            // Tạo thư mục tạm để lưu trữ tiện ích mở rộng
            var tempDir = Path.Combine(profilePath, "chrome_proxy_auth_extension");
            Directory.CreateDirectory(tempDir);

            // Ghi các file vào thư mục tạm
            File.WriteAllText(Path.Combine(tempDir, "manifest.json"), manifestJson);
            File.WriteAllText(Path.Combine(tempDir, "background.js"), backgroundJs);

            return tempDir;
        }

        
    }
}
