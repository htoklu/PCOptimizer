@echo off
setlocal

echo ============================================
echo   PC Optimizer - Tek EXE Derleme Aracı
echo ============================================
echo.

REM .NET SDK kurulu mu kontrol et
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo HATA: .NET SDK bulunamadi.
    echo Lutfen once şu adresten .NET 8 SDK kurun:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK bulundu, derleme başlıyor...
echo.

REM Kaynak koddan tek dosyalık, kendi kendine yeten exe üret
cd /d "%~dp0src\PCOptimizer"
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%~dp0output"

if %errorlevel% neq 0 (
    echo.
    echo HATA: Derleme basarisiz oldu. Yukaridaki mesaji kontrol edin.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   BASARILI!
echo   EXE dosyasi burada: %~dp0output\PCOptimizer.exe
echo ============================================
echo.

REM Karışmasın diye ayrı, temiz bir klasöre kopyala (masaüstünde değil,
REM proje klasörü altında ayrı "output" klasöründe duruyor)
echo Sadece exe'yi kullanmak icin "output" klasorunu açabilirsiniz.
echo.

pause
