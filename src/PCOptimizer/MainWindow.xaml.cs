using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace PCOptimizer
{
    public partial class MainWindow : Window
    {
        private const string KofiUrl = "https://ko-fi.com/htoklu";
        private const string SupportEmail = "htoklu1453@gmail.com";
        private const string Iban = "TR910006701000000057870794";
        private const string UsdtAddress = "TPRPEBtS8YbTnETdzFSNQezTNeqKfHtsLY";

        private readonly ObservableCollection<OptimizationStep> _steps;

        public MainWindow()
        {
            InitializeComponent();

            _steps = new ObservableCollection<OptimizationStep>(OptimizerService.GetAllSteps());
            StepsList.ItemsSource = _steps;

            if (!OptimizerService.IsAdministrator())
            {
                StatusText.Text = "UYARI: Uygulama yönetici olarak çalışmıyor. Bazı ayarlar uygulanamayabilir. " +
                                   "Uygulamayı kapatıp 'Yönetici olarak çalıştır' ile yeniden açın.";
                StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            });
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!OptimizerService.IsAdministrator())
            {
                MessageBox.Show("Bu işlem için yönetici yetkisi gerekiyor. Lütfen uygulamayı 'Yönetici olarak çalıştır' ile açın.",
                    "Yönetici Gerekli", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Seçilen optimizasyonlar uygulanacak. Mevcut ayarların yedeği otomatik alınacak ve istediğin zaman 'Eski Haline Döndür' ile geri dönebileceksin. Devam edilsin mi?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            ApplyButton.IsEnabled = false;
            RestoreButton.IsEnabled = false;
            LogText.Text = "";

            foreach (var step in _steps)
            {
                if (!step.IsSelected) continue;
                try
                {
                    Log($"Uygulanıyor: {step.Name} ...");
                    step.Apply(Log);
                }
                catch (Exception ex)
                {
                    Log($"HATA ({step.Name}): {ex.Message}");
                }
            }

            Log("Tamamlandı. Bazı ayarların etkili olması için bilgisayarı yeniden başlatman önerilir.");
            ApplyButton.IsEnabled = true;
            RestoreButton.IsEnabled = true;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!OptimizerService.IsAdministrator())
            {
                MessageBox.Show("Bu işlem için yönetici yetkisi gerekiyor. Lütfen uygulamayı 'Yönetici olarak çalıştır' ile açın.",
                    "Yönetici Gerekli", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Tüm optimizasyonlar, uygulanmadan önceki haline geri döndürülecek. Devam edilsin mi?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            ApplyButton.IsEnabled = false;
            RestoreButton.IsEnabled = false;
            LogText.Text = "";

            foreach (var step in _steps)
            {
                try
                {
                    Log($"Geri alınıyor: {step.Name} ...");
                    step.Restore(Log);
                }
                catch (Exception ex)
                {
                    Log($"HATA ({step.Name}): {ex.Message}");
                }
            }

            Log("Geri alma tamamlandı. Yeniden başlatman önerilir.");
            ApplyButton.IsEnabled = true;
            RestoreButton.IsEnabled = true;
        }

        // ---------------------------------------------------------------
        // Destek / Support butonları
        // ---------------------------------------------------------------
        private void KofiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = KofiUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı açılamadı: {ex.Message}\n\n{KofiUrl}", "Hata");
            }
        }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"mailto:{SupportEmail}?subject=PC%20Optimizer%20Destek",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"E-posta istemcisi açılamadı: {ex.Message}\n\n{SupportEmail}", "Hata");
            }
        }

        private void IbanButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Iban);
            MessageBox.Show("IBAN panoya kopyalandı:\n" + Iban, "Kopyalandı",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UsdtButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(UsdtAddress);
            MessageBox.Show("USDT (TRC20) adresi panoya kopyalandı:\n" + UsdtAddress, "Kopyalandı",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
