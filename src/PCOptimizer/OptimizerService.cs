using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;

namespace PCOptimizer
{
    /// <summary>
    /// Tek bir optimizasyon adımını temsil eder.
    /// Her adımın kendi Apply ve Restore mantığı vardır, böylece
    /// kullanıcı istediği anda geri alabilir.
    /// </summary>
    public class OptimizationStep
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsSelected { get; set; } = true;

        public Action<Action<string>> Apply { get; set; } = _ => { };
        public Action<Action<string>> Restore { get; set; } = _ => { };
    }

    public static class OptimizerService
    {
        private static readonly string BackupFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer", "Backup");

        private static readonly string BackupFile = Path.Combine(BackupFolder, "backup.json");

        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // ---------------------------------------------------------------
        // Yardımcı: komut satırı programı çalıştır (powercfg, netsh, vs.)
        // ---------------------------------------------------------------
        private static string RunCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                string output = proc!.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                return string.IsNullOrWhiteSpace(error) ? output : output + " | " + error;
            }
            catch (Exception ex)
            {
                return $"HATA: {ex.Message}";
            }
        }

        // ---------------------------------------------------------------
        // Registry yedekleme yardımcıları
        // ---------------------------------------------------------------
        private static Dictionary<string, Dictionary<string, object?>> _backupData = new();

        private static void LoadBackup()
        {
            Directory.CreateDirectory(BackupFolder);
            if (File.Exists(BackupFile))
            {
                var json = File.ReadAllText(BackupFile);
                _backupData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(json)
                              ?? new();
            }
        }

        private static void SaveBackup()
        {
            Directory.CreateDirectory(BackupFolder);
            var json = JsonSerializer.Serialize(_backupData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BackupFile, json);
        }

        private static void BackupRegistryValue(string groupKey, string hive, string subKey, string valueName)
        {
            LoadBackup();
            if (!_backupData.ContainsKey(groupKey))
                _backupData[groupKey] = new Dictionary<string, object?>();

            object? current = null;
            try
            {
                using var key = OpenBaseKey(hive).OpenSubKey(subKey, false);
                current = key?.GetValue(valueName);
            }
            catch { /* değer yoksa null kalır, geri alırken silinir */ }

            string entryKey = $"{hive}|{subKey}|{valueName}";
            if (!_backupData[groupKey].ContainsKey(entryKey))
            {
                _backupData[groupKey][entryKey] = current is null ? "__NULL__" : current;
                SaveBackup();
            }
        }

        private static RegistryKey OpenBaseKey(string hive) => hive switch
        {
            "HKCU" => Registry.CurrentUser,
            "HKLM" => Registry.LocalMachine,
            _ => throw new ArgumentException("Bilinmeyen hive: " + hive)
        };

        private static void RestoreGroup(string groupKey, Action<string> log)
        {
            LoadBackup();
            if (!_backupData.TryGetValue(groupKey, out var entries))
            {
                log($"[{groupKey}] için yedek bulunamadı, atlanıyor.");
                return;
            }

            foreach (var kv in entries)
            {
                var parts = kv.Key.Split('|', 3);
                if (parts.Length != 3) continue;
                var (hive, subKey, valueName) = (parts[0], parts[1], parts[2]);

                try
                {
                    using var key = OpenBaseKey(hive).CreateSubKey(subKey, true);
                    if (kv.Value is JsonElement je && je.ValueKind == JsonValueKind.String && je.GetString() == "__NULL__")
                    {
                        key?.DeleteValue(valueName, false);
                    }
                    else if (kv.Value is JsonElement je2)
                    {
                        object valueToSet = je2.ValueKind switch
                        {
                            JsonValueKind.Number => je2.GetInt64(),
                            _ => je2.GetString() ?? ""
                        };
                        key?.SetValue(valueName, valueToSet);
                    }
                    log($"Geri alındı: {subKey}\\{valueName}");
                }
                catch (Exception ex)
                {
                    log($"Geri alma hatası ({subKey}\\{valueName}): {ex.Message}");
                }
            }
        }

        // ---------------------------------------------------------------
        // OPTİMİZASYON ADIMLARI
        // Her biri gerçekten belgelenmiş, bilinen Windows ayarlarıdır.
        // Mucizevi "gizli" bir ayar yoktur; şeffaf ve geri alınabilirdir.
        // ---------------------------------------------------------------
        public static List<OptimizationStep> GetAllSteps()
        {
            return new List<OptimizationStep>
            {
                new OptimizationStep
                {
                    Id = "power_plan",
                    Name = "Güç Planı: Yüksek Performans",
                    Description = "Windows güç planını 'Yüksek Performans' moduna alır. CPU'nun düşük güç durumlarına inmesini geciktirerek daha tutarlı FPS sağlar.",
                    Apply = log =>
                    {
                        // Mevcut aktif planı yedekle
                        var current = RunCommand("powercfg", "/getactivescheme");
                        LoadBackup();
                        _backupData["power_plan"] = new Dictionary<string, object?> { ["active_scheme_output"] = current };
                        SaveBackup();

                        // Yüksek Performans planını etkinleştir (yerleşik GUID)
                        var result = RunCommand("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                        log("Güç planı 'Yüksek Performans' olarak ayarlandı. " + result);
                    },
                    Restore = log =>
                    {
                        // Dengeli plana geri dön (güvenli varsayılan)
                        var result = RunCommand("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                        log("Güç planı 'Dengeli' moduna geri alındı. " + result);
                    }
                },

                new OptimizationStep
                {
                    Id = "system_responsiveness",
                    Name = "Multimedia Sistem Yanıt Ayarları",
                    Description = "SystemResponsiveness değerini 0'a çeker ve 'Games' görev profiline yüksek CPU/GPU önceliği verir. Oyun sırasında arka plan görevlerinin kaynak çalmasını azaltır.",
                    Apply = log =>
                    {
                        BackupRegistryValue("system_responsiveness", "HKLM",
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness");
                        BackupRegistryValue("system_responsiveness", "HKLM",
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority");
                        BackupRegistryValue("system_responsiveness", "HKLM",
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority");
                        BackupRegistryValue("system_responsiveness", "HKLM",
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category");

                        using (var key = Registry.LocalMachine.CreateSubKey(
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true))
                            key?.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);

                        using (var key = Registry.LocalMachine.CreateSubKey(
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true))
                        {
                            key?.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            key?.SetValue("Priority", 6, RegistryValueKind.DWord);
                            key?.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                        }
                        log("Multimedia sistem yanıt ayarları oyun odaklı hale getirildi.");
                    },
                    Restore = log => RestoreGroup("system_responsiveness", log)
                },

                new OptimizationStep
                {
                    Id = "nagle",
                    Name = "Nagle Algoritmasını Kapat (TCP Gecikme Azaltma)",
                    Description = "Ağ arayüzü için TcpAckFrequency ve TCPNoDelay ayarlarını değiştirir. Küçük paketlerin (oyun input verisi gibi) beklemeden gönderilmesini sağlar, ping dalgalanmasını azaltabilir.",
                    Apply = log =>
                    {
                        const string baseKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                        using var interfaces = Registry.LocalMachine.OpenSubKey(baseKey, false);
                        if (interfaces == null) { log("Ağ arayüzleri bulunamadı."); return; }

                        foreach (var subName in interfaces.GetSubKeyNames())
                        {
                            string path = $@"{baseKey}\{subName}";
                            BackupRegistryValue("nagle", "HKLM", path, "TcpAckFrequency");
                            BackupRegistryValue("nagle", "HKLM", path, "TCPNoDelay");

                            using var sub = Registry.LocalMachine.OpenSubKey(path, true);
                            // Sadece gerçek bir IP'si olan arayüzlere uygula
                            if (sub?.GetValue("DhcpIPAddress") != null || sub?.GetValue("IPAddress") != null)
                            {
                                sub.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                sub.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                            }
                        }
                        log("Nagle algoritması aktif ağ arayüzlerinde devre dışı bırakıldı.");
                    },
                    Restore = log => RestoreGroup("nagle", log)
                },

                new OptimizationStep
                {
                    Id = "tcp_autotuning",
                    Name = "TCP Otomatik Ayarlamayı Normalleştir",
                    Description = "netsh ile TCP receive window auto-tuning seviyesini 'normal'e ayarlar. Bazı router/modemlerle yaşanan ani gecikme sıçramalarını azaltabilir.",
                    Apply = log =>
                    {
                        var before = RunCommand("netsh", "int tcp show global");
                        LoadBackup();
                        _backupData["tcp_autotuning"] = new Dictionary<string, object?> { ["before_output"] = before };
                        SaveBackup();

                        var result = RunCommand("netsh", "int tcp set global autotuninglevel=normal");
                        log("TCP auto-tuning seviyesi 'normal' yapıldı. " + result);
                    },
                    Restore = log =>
                    {
                        var result = RunCommand("netsh", "int tcp set global autotuninglevel=normal");
                        log("TCP auto-tuning ayarı varsayılana (normal) döndürüldü. " + result);
                    }
                },

                new OptimizationStep
                {
                    Id = "game_mode",
                    Name = "Windows Oyun Modu'nu Etkinleştir",
                    Description = "Windows'un yerleşik Game Mode özelliğini açık hale getirir; oyun çalışırken güncelleme ve bildirimleri arka plana atar.",
                    Apply = log =>
                    {
                        BackupRegistryValue("game_mode", "HKCU", @"Software\Microsoft\GameBar", "AutoGameModeEnabled");
                        BackupRegistryValue("game_mode", "HKCU", @"Software\Microsoft\GameBar", "AllowAutoGameMode");

                        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar", true);
                        key?.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
                        key?.SetValue("AllowAutoGameMode", 1, RegistryValueKind.DWord);
                        log("Windows Oyun Modu etkinleştirildi.");
                    },
                    Restore = log => RestoreGroup("game_mode", log)
                },

                new OptimizationStep
                {
                    Id = "hags",
                    Name = "Donanım Hızlandırmalı GPU Zamanlama (HAGS)",
                    Description = "Hardware-accelerated GPU Scheduling'i açar. Modern GPU/sürücülerde giriş gecikmesini biraz azaltabilir. Bazı eski GPU'larda etkisiz olabilir; bu yüzden isteğe bağlıdır.",
                    IsSelected = false, // GPU'ya göre değişken etki, varsayılan kapalı bırakılıyor
                    Apply = log =>
                    {
                        BackupRegistryValue("hags", "HKLM",
                            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
                        using var key = Registry.LocalMachine.CreateSubKey(
                            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true);
                        key?.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                        log("HAGS etkinleştirildi. (Etkili olması için yeniden başlatma gerekir.)");
                    },
                    Restore = log => RestoreGroup("hags", log)
                },

                new OptimizationStep
                {
                    Id = "network_throttling",
                    Name = "Ağ Kısıtlama İndeksini Kapat (NetworkThrottlingIndex)",
                    Description = "Windows, multimedia dışı ağ trafiğine varsayılan olarak bir sınır koyar (MMCSS). Bu sınırı kaldırmak, oyun sırasında ağ paketlerinin gecikmeden işlenmesine yardımcı olabilir.",
                    Apply = log =>
                    {
                        BackupRegistryValue("network_throttling", "HKLM",
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
                        using var key = Registry.LocalMachine.CreateSubKey(
                            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true);
                        key?.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        log("Ağ kısıtlama indeksi kaldırıldı.");
                    },
                    Restore = log => RestoreGroup("network_throttling", log)
                },

                new OptimizationStep
                {
                    Id = "game_dvr",
                    Name = "Game Bar Arka Plan Kaydını (Game DVR) Kapat",
                    Description = "Xbox Game Bar arka planda sürekli bir kayıt tamponu tutar; bu GPU ve RAM kaynağı kullanır. Kapatmak oyun sırasında ekstra kaynak boşaltır.",
                    Apply = log =>
                    {
                        BackupRegistryValue("game_dvr", "HKCU", @"System\GameConfigStore", "GameDVR_Enabled");
                        BackupRegistryValue("game_dvr", "HKLM", @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR");

                        using (var key = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore", true))
                            key?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);

                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", true))
                            key?.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);

                        log("Game DVR arka plan kaydı kapatıldı.");
                    },
                    Restore = log => RestoreGroup("game_dvr", log)
                },

                new OptimizationStep
                {
                    Id = "pointer_precision",
                    Name = "Fare İşaretçi Hassasiyetini (Pointer Precision) Kapat",
                    Description = "Windows'un fare hareketine uyguladığı ivmelendirmeyi (acceleration) kapatır. Ham ve tutarlı fare hareketi ister çoğu FPS oyuncusu bunu manuel kapatır; nişan tutarlılığını artırabilir.",
                    IsSelected = false, // Kişisel tercihe bağlı, bazı oyuncular ivmelendirmeyi sever
                    Apply = log =>
                    {
                        BackupRegistryValue("pointer_precision", "HKCU", @"Control Panel\Mouse", "MouseSpeed");
                        BackupRegistryValue("pointer_precision", "HKCU", @"Control Panel\Mouse", "MouseThreshold1");
                        BackupRegistryValue("pointer_precision", "HKCU", @"Control Panel\Mouse", "MouseThreshold2");

                        using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse", true);
                        key?.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key?.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key?.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                        log("Fare ivmelendirmesi kapatıldı. (Etkili olması için oturumu kapatıp açman gerekebilir.)");
                    },
                    Restore = log => RestoreGroup("pointer_precision", log)
                },

                new OptimizationStep
                {
                    Id = "fse_preference",
                    Name = "Tam Ekran Optimizasyonu Sistem Tercihi",
                    Description = "Windows'un tam ekran oyunlara yaklaşımını, exclusive fullscreen'e izin verecek şekilde ayarlar. Bazı oyunlarda (özellikle rekabetçi FPS'lerde) input lag'i azaltabilir.",
                    Apply = log =>
                    {
                        BackupRegistryValue("fse_preference", "HKCU", @"System\GameConfigStore", "GameDVR_FSEBehaviorMode");
                        BackupRegistryValue("fse_preference", "HKCU", @"System\GameConfigStore", "GameDVR_FSEBehavior");

                        using var key = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore", true);
                        key?.SetValue("GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord);
                        key?.SetValue("GameDVR_FSEBehavior", 2, RegistryValueKind.DWord);
                        log("Tam ekran optimizasyonu tercihi güncellendi.");
                    },
                    Restore = log => RestoreGroup("fse_preference", log)
                },
            };
        }

        public static void ClearBackup()
        {
            if (File.Exists(BackupFile))
                File.Delete(BackupFile);
            _backupData.Clear();
        }
    }
}
