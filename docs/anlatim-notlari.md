# PromptLab – Anlatım Notları

Bu dosya, ters anlatım çalışmasında (Hafta 3, Bölüm 4) birlikte yazılan notları tutar. Her soruya kodda ilgili yeri bulup **kendi cümlelerimizle** kısa bir cevap yazıyoruz. Her parça için üç soru: **Ne yapıyor? Neden böyle? Silinirse ne olur?**

- Bilal anlatır: Burhan'ın entity'leri (Comparison, ComparisonResult, Vote, ModelStat, enum'lar), Program.cs'teki Identity ve Swagger kayıtları, DbSeeder, ModelCatalog DTO'ları ve API controller'ı, role göre menü.
- Burhan anlatır: Bilal'in entity'leri (ApplicationUser, ModelCatalog, UserApiKey), ApplicationDbContext, view model'ler, AccountController, giriş ve kayıt view'ları.

## Bir isteğin yolculuğu

### Giriş formunun gönderilmesi (POST /Account/Login)
<!-- Tarayıcı → Kestrel → middleware → routing → model binding → doğrulama → Identity → EF Core → cookie → 302 -->

### Swagger'dan model ekleme (POST /api/ModelCatalog)
<!-- rota, [ApiController], DTO doğrulaması, yetki, SaveChangesAsync, 201 -->

## Konu 1: Proje yapısı ve çalıştırma
- `dotnet run` dediğimizde ne oluyor?
- `.csproj` dosyasında ne var?
- Bağlantı dizesi neden appsettings.json'da değil?
- Veritabanı neden 5433 portunda?
- Klasörler neyi tutuyor?

## Konu 2: Program.cs
- `builder.Build()` öncesi ve sonrası neden ayrı?
- Dependency injection nedir? (Kayıt Program.cs'te, enjeksiyon constructor'da.)
- AddScoped / AddTransient / AddSingleton farkı?
- UseAuthentication neden UseAuthorization'dan önce?
- Seed neden CreateScope içinde çağrılıyor?

## Konu 3: Veri katmanı
- Entity, DbSet, DbContext nedir?
- ApplicationDbContext neden IdentityDbContext'ten türüyor?
- Migration nedir?
- Navigation property ile foreign key farkı?
- ComparisonResult → ModelCatalog ilişkisi neden Restrict?
- Tarihlerde neden DateTime.UtcNow?

## Konu 4: Kimlik (Identity)
- Şifre veritabanında nasıl duruyor?
- Giriş yapan kullanıcı sonraki isteklerde nasıl tanınıyor?
- Roller nerede tutuluyor?
- Hesap kilitleme nasıl çalışıyor?
- Seed neden kayıtları çoğaltmıyor?

## Konu 5: MVC akışı
- Model binding nedir?
- İstemci tarafı ve sunucu tarafı doğrulama farkı?
- Antiforgery anahtarı ne işe yarıyor?
- returnUrl neden kontrol ediliyor?
- Post/Redirect/Get neden?

## Konu 6: API
- MVC controller ile API controller farkı?
- DTO neden kullanıldı?
- Durum kodları (200, 201, 204, 400, 401, 403, 404, 409)?
- Swagger nedir?
- Giriş yapmadan korumalı API'ye istek atınca neden 401 değil yönlendirme geliyor?

## Konu 7: Ön yüz
- Layout nedir?
- Tag helper nedir?
- `_ViewImports.cshtml` ne işe yarıyor?
- Menü role göre nasıl değişiyor?

## Konu 8: Git
- Neden dört dal var?
- PR'dan önce neden `git pull origin develop`?
- Çakışma nasıl çözülüyor?

## Uygulamalı çalışmalar
- [ ] Debugger: POST Login'e breakpoint
- [ ] Tarayıcı araçları: antiforgery alanı, 302 akışı, Identity cookie'si
- [ ] pgAdmin: PasswordHash, AccessFailedCount
- [ ] Swagger hataları: 400, 409, 404
- [ ] Bozup gözlemleme: `app.UseAuthentication();` yoruma alma

## Açık sorular
<!-- İkimizin de cevaplayamadığı sorular; ayrıca GitHub issue'suna yazılır. -->

## 3. hafta kodu (hafta sonu tekrarı)
- Bilal anlatır: OpenRouterClient, ModelSyncService, test uç noktası.
- Burhan anlatır: ApiKeyService, API anahtarı sayfası, admin model paneli.
