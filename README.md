# PC Optimizer 🎮⚡

Valorant, CS2 ve benzeri rekabetçi FPS oyunlarında düşük gecikme (lag) ve
kararlı FPS için Windows 11'in **gerçek, Microsoft tarafından belgelenmiş**
ayarlarını tek tıkla uygulayan açık kaynaklı bir masaüstü aracı.

## Neden bu araç?

İnternette dolaşan "PC hızlandırıcı" uygulamalarının çoğu abartılı vaatlerde
bulunur ve ne yaptığını gizler. Bu proje tam tersini hedefliyor:

- ✅ **Şeffaf** — her ayarın ne işe yaradığı arayüzde açıkça yazıyor
- ✅ **Geri alınabilir** — her değişiklikten önce otomatik yedek alınır,
  tek tıkla eski hâline dönebilirsin
- ✅ **Açık kaynak** — kodun tamamı burada, ne yaptığını kendin doğrulayabilirsin
- ❌ **Mucize yok** — donanımın belirlediği fiziksel sınırı hiçbir ayar aşamaz

## Neler yapıyor?

- Güç planını Yüksek Performans'a alır
- Multimedia/oyun görev önceliklerini yükseltir (SystemResponsiveness)
- Nagle algoritmasını kapatır (TCP gecikme azaltma)
- TCP auto-tuning seviyesini normalleştirir
- Windows Game Mode'u açar
- Ağ kısıtlama indeksini (NetworkThrottlingIndex) kaldırır
- Game Bar arka plan kaydını (Game DVR) kapatır
- (isteğe bağlı) Hardware GPU Scheduling (HAGS) açar
- (isteğe bağlı) Fare işaretçi ivmelendirmesini kapatır
- Tam ekran optimizasyonu sistem tercihini ayarlar

Her ayarın öncesinde otomatik yedek alınır; uygulama içindeki
**"Eski Haline Döndür"** butonuyla tüm değişiklikler tek tıkla geri alınabilir.

## Kurulum

### Seçenek 1 — Hazır exe (önerilen)
1. [Releases](https://github.com/htoklu/PCOptimizer/releases) sekmesinden
   en son sürümün `.exe` dosyasını indir
2. Dosyaya sağ tık → **Yönetici olarak çalıştır**
   (registry değiştirdiği için admin yetkisi şart)

> Windows SmartScreen "Tanınmayan yayıncı" uyarısı gösterebilir çünkü
> uygulama ücretli bir sertifikayla imzalanmamıştır. Bu normal bir durumdur —
> **Daha fazla bilgi → Yine de çalıştır** ile devam edebilirsin. Kodun tamamı
> bu repoda açık, istersen incele.

### Seçenek 2 — Kaynaktan kendin derle
1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) kur
2. Bu repoyu indir/klonla
3. `build.bat` dosyasına çift tıkla
4. `output\PCOptimizer.exe` otomatik oluşur

## Kullanım

1. Uygulamayı yönetici olarak aç
2. İstediğin ayarları işaretle (checkbox)
3. **"Seçilenleri Uygula"** butonuna bas
4. Memnun kalmazsan **"Eski Haline Döndür"** ile tek tıkla geri al

## Proje yapısı
PCOptimizer/
├── build.bat ← çift tıkla, tek exe üretir
├── src/PCOptimizer/
│ ├── PCOptimizer.csproj
│ ├── app.manifest ← admin yetkisi ister
│ ├── App.xaml / .cs
│ ├── MainWindow.xaml / .cs ← arayüz
│ └── OptimizerService.cs ← optimizasyon + yedekleme mantığı
└── output/ ← build sonrası exe burada oluşur

## Katkı

Pull request'lere açığım. Hata bulursan veya yeni (gerçek, belgelenmiş)
bir optimizasyon önerin varsa bir Issue açabilirsin.

## 💰 Support

Bu araç işine yaradıysa geliştirilmeye devam etmesine destek olabilirsin:

| Yöntem | Bilgi |
|---|---|
| ☕ Ko-fi | [ko-fi.com/htoklu](https://ko-fi.com/htoklu) |
| 💳 IBAN (Yapı Kredi) | `TR910006701000000057870794` |
| ₿ USDT (TRC20) | `TPRPEBtS8YbTnETdzFSNQezTNeqKfHtsLY` |
| 📧 Email | htoklu1453@gmail.com |

## Sorumluluk Reddi

Bu araç sistem ayarlarını (registry, güç planı, ağ ayarları) değiştirir.
Kullanmadan önce ne yaptığını anlamak için kodu incelemeni öneririz.
Herhangi bir zarardan geliştirici sorumlu tutulamaz — ancak tüm değişiklikler
yedeklenip geri alınabilir şekilde tasarlanmıştır.
