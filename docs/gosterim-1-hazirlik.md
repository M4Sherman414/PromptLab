# 1. Gösterim Hazırlığı (2-4. haftalar)

Bu gösterim, Bilal'in liderlik ettiği 2-4. haftaları kapsar: kimlik doğrulama, model kataloğu, OpenRouter bağlantısı, API anahtarı ve normal mod (sohbet).

## Gösterimden önce kontrol listesi

- [ ] Docker Desktop açık, `promptlab-db` konteyneri çalışıyor (`docker start promptlab-db`)
- [ ] `git pull origin develop` sonrası `dotnet ef database update` çalıştırıldı (`AddChatEntities` uygulanmış olmalı)
- [ ] `dotnet run` ile uygulama hatasız açılıyor
- [ ] Admin hesabıyla giriş yapılabiliyor (user-secrets'taki `AdminUser`)
- [ ] Admin panelinden "Senkronize et" çalıştırıldı, model listesi dolu
- [ ] Demo kullanıcısının geçerli bir OpenRouter anahtarı var, hesapta küçük bir kredi yüklü
- [ ] Demo için 1-2 ücretsiz model seçildi ve önceden denendi (429 kotasına takılmıyor)
- [ ] **Yedek:** Önceden yapılmış 2-3 mesajlık bir sohbet "Sohbetlerim" listesinde duruyor (model canlı cevap vermezse bu gösterilir)
- [ ] Tarayıcıda pgAdmin ve Swagger sekmeleri hazır

## Demo senaryosu (yaklaşık 10 dakika)

| # | Adım | Kim anlatır | Gösterilecek nokta |
|---|------|-------------|--------------------|
| 1 | Yeni kullanıcı kaydı, çıkış, tekrar giriş | Bilal | Doğrulama mesajları, 5 hatalı denemede kilitleme |
| 2 | Normal kullanıcı ve admin menüsü farkı | Burhan | Role göre menü, pasif bağlantılar |
| 3 | Swagger'da ModelCatalog API'si | Burhan | Yetkisiz isteğin 401/403 alması |
| 4 | Admin paneli: senkronize et, ara, modeli pasifleştir | Bilal | Pasif model sohbet ekranındaki listede görünmez |
| 5 | API anahtarı olmadan ana sayfa (Yeni sohbet) | Bilal | Kutunun kapalı olması, anahtar sayfasına bağlantı ve "kaldığınız sayfaya döneceksiniz" bilgisi |
| 6 | Önce hatalı, sonra doğru anahtar kaydetme | Bilal | Hatalı anahtarın OpenRouter tarafından reddedilip kaydedilmemesi; doğru anahtarda "geçerli" rozeti, usage/limit, ana sayfaya dönüş |
| 7 | pgAdmin'de `UserApiKeys` tablosu | Bilal | Anahtarın şifreli hali (Data Protection) |
| 8 | Ana sayfada model seç, system prompt yaz, ilk mesajı gönder; 3-4 mesaj devam et | Bilal | Mesaj hemen görünür, ardından "yazıyor…", sonra cevap (sayfa yenilenmez) |
| 9 | Her cevabın altındaki ölçümler | Bilal | **Input token sayısının her mesajda büyümesi (+N)**, output tokens, latency, cost, üstteki toplamlar |
| 10 | Sohbet servisinin içi | Burhan | Geçmişin mesaj listesine çevrilip OpenRouter'a gönderilmesi, usage kaydı |
| 11 | Sohbetlerim listesi ve eski bir sohbete devam | Burhan | Başlığın ilk mesajdan oluşması, son güncellenene göre sıralama |
| 12 | pgAdmin'de `ChatMessages` tablosu | Burhan | `InputTokens`, `OutputTokens`, `LatencyMs`, `EstimatedCost` alanları |

## Sorulabilecek sorular ve kısa cevaplar

- **Input token sayısı neden her mesajda artıyor?** Modelin hafızası yok. Her istekte system prompt ve bütün geçmiş yeniden gönderiliyor (`ChatService.BuildOpenRouterMessages`), bu yüzden input token birikiyor.
- **API anahtarı nasıl korunuyor?** `ApiKeyService` anahtarı ASP.NET Core Data Protection ile şifreleyip saklar. Ekranda yalnızca son 4 karakter gösterilir. Anahtar yalnızca istek anında çözülür.
- **Anahtarı olmayan kullanıcı nasıl yönlendiriliyor?** `Filters/RequireApiKeyAttribute.cs` içindeki action filter, işlemden önce anahtarı kontrol eder. Anahtar yoksa `returnUrl` ile anahtar sayfasına gönderir. AJAX isteğinde yönlendirme yerine 409 ve JSON döner. Bu filtre 5-7. haftalarda karşılaştırma ve turnuva ekranlarında da kullanılacak.
- **Açık yönlendirme (open redirect) riski?** `returnUrl` yalnızca `Url.IsLocalUrl` doğrularsa kullanılır, `LocalRedirect` ile yönlendirilir. Dış bir adres verilirse yok sayılır.
- **Hatalı anahtar nasıl en baştan yakalanıyor?** Anahtar kaydedilmeden önce OpenRouter'ın `GET /api/v1/key` ucuna sorulur (kredi harcamaz). 401 dönerse kaydedilmez. Kayıtlı anahtar sonradan iptal edilirse ana sayfa, sohbet ekranı ve anahtar sayfası bunu gösterir. Sonuç 10 dakika önbellekte tutulur (`ApiKeyService.CheckStoredKeyAsync`).
- **Ana sayfa neden sohbet ekranı?** Ekran taslağında Home ve New Chat aynı ekran. Kullanıcı modeli seçip ilk mesajı yazar, `Chat/Create` sohbeti oluşturur ve ilk mesaj sohbet sayfasında otomatik gönderilir.
- **Mesaj sayfa yenilenmeden nasıl geliyor?** `wwwroot/js/chat.js` kullanıcının mesajını hemen ekrana ekler ve formu `fetch` ile gönderir. Model cevap verene kadar "yazıyor…" görünür. Sunucu asistan cevabını `_ChatMessage` partial view'ı olarak HTML döner ve bu HTML listeye eklenir. İstek başarısız olursa mesaj balonu kaldırılır ve metin kutuya geri konur. JavaScript kapalıysa form normal POST ile çalışmaya devam eder.
- **Maliyet nereden geliyor?** OpenRouter cevabındaki `usage.cost` alanından. Bu alan yoksa model kataloğundaki milyon token fiyatından hesaplanır.
- **Streaming neden yok?** Plan gereği 5. haftada ekleniyor. Bu hafta cevap model bitirince tek seferde geliyor. Sayfa AJAX'a geçtiği için streaming'e geçiş kolaylaşacak.

## Riskler

- **429 (ücretsiz model kotası):** Başka bir ücretsiz modele geçilir ya da yedek sohbet gösterilir.
- **İnternet yok:** Önceden hazırlanmış sohbetler ve pgAdmin üzerinden kayıtlar gösterilir.
