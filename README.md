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
* .NET 10.0 SDK
* PostgreSQL / Docker
