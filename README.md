# PC Optimizer — Oyun Performans Aracı

Valorant, CS2 gibi oyunlar için **gerçek, belgelenmiş ve geri alınabilir**
Windows ayarlarını tek tıkla uygulayan bir masaüstü uygulaması.

## Önemli — dürüst bir not

Bu uygulama "gizli mucize hızlandırma scripti" değildir. İnternette dolaşan
çoğu "FPS booster" uygulaması abartılı vaatlerde bulunur. Burada sadece
gerçekten belgelenmiş, Microsoft tarafından tanımlı ayarlar kullanılıyor:

- Güç planını Yüksek Performans yapmak
- Multimedia/oyun görev önceliklerini yükseltmek
- Nagle algoritmasını kapatmak (küçük paketlerde gecikmeyi azaltır)
- TCP auto-tuning seviyesini normalleştirmek
- Windows Game Mode'u açmak
- (isteğe bağlı) Hardware GPU Scheduling'i açmak
- Ağ kısıtlama indeksini (NetworkThrottlingIndex) kaldırmak
- Game Bar arka plan kaydını (Game DVR) kapatmak
- (isteğe bağlı) Fare işaretçi ivmelendirmesini kapatmak
- Tam ekran optimizasyonu sistem tercihini ayarlamak

Uygulamanın alt kısmında geliştiriciye destek olmak isteyenler için
Ko-fi, e-posta, IBAN ve USDT (TRC20) bilgileri yer alıyor — tamamen isteğe bağlı.

Donanım donanımdır — zayıf bir GPU'yu bu ayarlar güçlü yapmaz. Ama bu
ayarlar gerçekten CPU/ağ kaynaklı mikro-takılmaları ve gecikme
dalgalanmalarını azaltabilir.

**Her ayar, uygulanmadan önce otomatik yedeklenir** ve uygulama içindeki
"Eski Haline Döndür" butonuyla tek tıkla geri alınabilir.

## Gereksinimler

- Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (sadece derleme için, kurulumu ücretsiz ve 2 dakika sürer)

## Nasıl derlenir (tek tık)

1. Bu klasörü (PCOptimizer) bilgisayarına indir / zip'i çıkar.
2. `build.bat` dosyasına **çift tıkla**.
3. Script otomatik olarak derler ve `output` klasörüne
   tek bir `PCOptimizer.exe` dosyası koyar. Kaynak kodla karışmaz,
   ayrı klasörde durur.
4. `output\PCOptimizer.exe` dosyasına sağ tıklayıp
   **"Yönetici olarak çalıştır"** seç (registry değiştirdiği için admin gerekir).

## Masaüstüne kısayol koymak istersen

`output\PCOptimizer.exe` dosyasına sağ tık → **Gönder → Masaüstü (kısayol oluştur)**.
Kısayola sağ tıklayıp Özellikler → Gelişmiş → "Yönetici olarak çalıştır"ı
işaretlersen her seferinde otomatik admin ile açılır.

## Klasör yapısı

```
PCOptimizer/
├── build.bat              ← çift tıkla, exe'yi üretir
├── README.md               ← bu dosya
├── src/PCOptimizer/         ← C# kaynak kodu (WPF)
│   ├── PCOptimizer.csproj
│   ├── app.manifest         ← admin yetkisi ister
│   ├── App.xaml / .cs
│   ├── MainWindow.xaml / .cs ← arayüz
│   └── OptimizerService.cs   ← optimizasyon + yedekleme mantığı
└── output/                  ← build.bat çalıştıktan sonra exe burada oluşur
```

## Cursor / VS Code ile geliştirmeye devam etmek istersen

`src/PCOptimizer` klasörünü Cursor'da aç, `dotnet run` ile hızlıca test
edebilirsin (admin terminalden çalıştırman gerekir çünkü registry'ye yazıyor).

## Kurulum

1. [Releases](../../releases) sekmesinden hazır `.exe` dosyasını indir,
   **veya** kaynak koddan kendin derle (`build.bat` çift tıkla, `.NET 8 SDK` gerekir)
2. Yönetici olarak çalıştır (registry değiştirdiği için admin yetkisi şart)
3. İstediğin ayarları işaretle, "Seçilenleri Uygula"ya bas
4. Memnun kalmazsan "Eski Haline Döndür" ile tek tıkla geri al
