# PromptLab

Prompt Lab, aynı prompt’u OpenRouter API üzerinden iki farklı yapay zekâ modeline aynı anda gönderip yanıtlarını yan yana kıyaslayan bir ASP.NET Core MVC web uygulamasıdır.

Modellerin ürettiği yanıtları akış halinde gösterirken arka planda gecikme süresi, token sayısı, üretim hızı ve maliyeti hesaplar. Model adlarının gizlendiği kör test modu sayesinde kullanıcının yaptığı oylamalarla modellere Elo puanı verir, ortalama metrikleri sıralama sayfasında toplar ve geçmiş aramaları saklar.

## Özellikler

- Kullanıcı kayıt olur.
- Openrouter'da hesap açar. Ve api-key ini sisteme yükler. Sistemde şifrelenmiş olarak saklanır.
- Dilediği modeli seçerek sohbet edebilir.(Normal Mode)
- Dilediği modelleri seçerek karşılaştırabilir.(Battle Mode)
- Modelleri karşılaştırırken performansına göre puanlayabilir.
- Modeller Leaderboard sekmesinde rankına göre sıralanır.
- ...

## Teknolojiler

- C#
- .NET
- PostgreSQL
- Docker/PgAdmin
- OpenRouter

## Kurulum

### Ön Gereksinimler

- .NET 10.0 SDK
- PostgreSQL / Docker

### Adımlar

#### Projeyi Yerel Ortamda Çalıştırma

Projeyi klonlayın:
```bash
git clone https://github.com/M4Sherman414/PromptLab/ -b main
cd PromptLab/
```

Projenin NuGet paketlerini indirmek ve Entity Framework Core araçlarını yüklemek için şu komutları çalıştırın:
```bash
dotnet restore
dotnet build # Projenin 1 kez olsun derlenmiş olması için gerekli.
dotnet tool install --global dotnet-ef
```

DB şifresini ve kullanıcı adını kendinize göre değiştirmek için:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "yeni_deger"
```

İlgili DB yi oluşturmak veya güncellemek için:
```bash
dotnet ef database update
```

Projeyi çalıştırıp denemek için:
```bash
dotnet build # hata var mı görmek için kullanılabilir.(optional)
dotnet run
```

#### OpenRouter API_KEY Kullanımı

- Öncelikle https://www.openrouter.ai adresinden bir kullanıcı hesabı açmalısınız

- Ardından API_KEY inizi sisteme kaydetmelisiniz.

- Sonrasında free veya ücretli yapayzekaları tercihinize göre kullanabilir karşılaştırabilirsiniz.

