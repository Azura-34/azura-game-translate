# Azura Game Translate

Windows masaüstü uygulaması: oyun altyazısının ekrandaki bölümünü OCR ile okur ve DeepL API üzerinden Türkçeye çeviriyi kayan pencerede gösterir.

## Kurulum ve kullanım

1. Releases bölümünden AzuraGameTranslate-Setup-v1.0.0.exe dosyasını indirin.
2. Kurulum dosyasını çalıştırın. Windows yönetici izni, Tesseract OCR bileşenini yüklemek için gerekir.
3. Uygulamayı açın ve DeepL API anahtarınızı girin. API anahtarı uygulamada kalıcı olarak saklanmaz.
4. Yeni DeepL API hesapları için DeepL API Developer / Pro seçeneğini kullanın. DeepL API Free seçeneği yalnızca bu eski plana hâlihazırda erişimi olan hesaplar içindir.
5. Altyazı alanını seç düğmesine basıp oyundaki altyazının bulunduğu bölgeyi çerçeveleyin.
6. Çeviriyi başlat düğmesine basın. Çeviri ekranda üstte duran pencerede görünür; pencereyi sürükleyerek taşıyabilirsiniz.
7. İşiniz bittiğinde Durdur düğmesine basın.

İsteğe bağlı olarak uygulama, Windows'ta Türkçe ses motoru yüklüyse çeviriyi sesli okuyabilir.

## DeepL API anahtarı

DeepL hesabında Account → API Keys & Limits → Create key yolunu izleyerek bir anahtar oluşturun. Yeni kullanıcılar için DeepL API Developer planı toplam 1 milyon karaktere kadar ücretsiz deneme kotası sunar; bu kota yenilenmez. Planların koşulları zamanla değişebilir; DeepL API planlarını kontrol edin: https://support.deepl.com/hc/en-us/articles/360021200939-DeepL-API-plans

DeepL API Developer / Pro seçeneği api.deepl.com uç noktasını; eski hesaba yönelik DeepL API Free seçeneği api-free.deepl.com uç noktasını kullanır.

API anahtarınızı parola gibi koruyun. Kaybolduysa veya paylaşıldıysa DeepL hesabınızdan devre dışı bırakıp yenisini oluşturun.

## Gizlilik

Ekran görüntüleri diske kaydedilmez. OCR ile okunan altyazı metni çeviri için DeepL'e gönderilir. API anahtarı uygulama çalışırken bellekte tutulur ve dosyaya yazılmaz. DeepL'in veri işleme koşulları, kullandığınız API planına bağlıdır.

## Gereksinimler

- Windows 10 veya 11, 64 bit
- İnternet bağlantısı ve geçerli DeepL API anahtarı
- Kurulum sırasında Windows yönetici izni (Tesseract OCR için)

## Kaynaktan derleme

Windows PowerShell'i bu klasörde açıp .\build.ps1 komutunu çalıştırın.

Betik NSIS 3.13 ve Tesseract OCR kurucusunu resmi kaynaklarından indirir, SHA-256 özetlerini doğrular ve dist klasöründe kurulum dosyası ile kaynak arşivini oluşturur. Uygulama Windows .NET Framework C# derleyicisiyle derlenir.

## Lisans

Bu projenin kaynak kodu MIT lisansı altındadır. Kurulum dosyası Tesseract OCR kurucusunu da içerir; üçüncü taraf bileşen bilgileri ve lisans bildirimi için THIRD-PARTY-NOTICES.md dosyasına bakın.