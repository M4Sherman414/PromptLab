# PromptLab – 1. Hafta Görev Dağılımı

- **Haftanın lideri:** Bilal Said Yılmaz
- **Üyeler:** Bilal Said Yılmaz, Mustafa Burhan Kıran (Burhan)
- **Video teslimi:** 25 Ekim 2026, 23:59

Git komutlarının ayrıntıları için `git-branch-rehberi.md` dosyasına bakın. Bu belge yalnızca işleri, sorumluları ve işlerin sırasını gösterir.

---

## 1. Haftanın hedefi: Videoda gösterilecekler

Videoda en az şunlar görünmeli:

1. **Uygulama:** Çalışan MVC uygulaması, yani ana sayfa ve ortak layout (Bootstrap, mobil uyumlu).
2. **Veritabanı:** pgAdmin'de oluşmuş tablolar.
3. **GitHub:** Deponun kendisi, dallar ve iki üyenin commit'leri.
4. **OpenRouter:** API denemesinin sonucu.
5. **Ekran taslakları.**
6. **2. hafta planı:** Bu hafta yetişmeyen bir iş varsa burada söylenir.

---

## 2. Görev dağılımı özeti

**Bilal**

- GitHub ayarları, collaborator daveti ve `.gitignore`
- MVC projesini oluşturma; `main`, `develop` ve `bilal` dallarını açma
- PostgreSQL (Docker) ve EF Core kurulumu, user-secrets
- `ApplicationDbContext` ile `ApplicationUser`, `ModelCatalog` ve `UserApiKey` entity'leri
- Tüm `DbSet` satırları ve ilk migration
- `develop → main` haftalık PR'ı ve video

**Burhan**

- `Comparison`, `ComparisonResult`, `Vote` ve `ModelStat` entity'leri
- OpenRouter hesabı, API denemesi ve streaming'de usage alanının kontrolü
- Ekran taslakları (giriş, karşılaştırma, sonuç, sıralama, admin)
- Ortak layout ve ana sayfa (Bootstrap, mobil uyumlu)
- README iskeleti ve kurulum adımları

Bilal liderlik yaptığı ve videoyu hazırladığı için iş yükü Burhan'a biraz kaydırıldı. Entity'lerin ikiye bölünmesi, iki üyenin de veritabanı kısmını bilmesini sağlıyor. Bu önemli, çünkü gösterimde herhangi bir üyeye herhangi bir kod sorulabilir.

---

## 3. Bağımlılıklar: Kim kimi bekliyor?

Aşağıdaki adımlar birbirine bağlıdır. Her adım bir öncekinin bitmesini bekler, bu yüzden bu adımlar önceliklidir.

1. **Bilal: İlk push.** Bilal `main`, `develop` ve `bilal` dallarını GitHub'a gönderir. Bundan sonra Burhan depoyu clone'layabilir.
2. **Bilal: DbContext ve kendi entity'leri.** Bilal bunları `bilal → develop` PR'ıyla gönderir, Burhan merge eder. Bundan sonra Burhan entity'lerini yazmaya başlayabilir, çünkü onun entity'leri `ApplicationUser` ve `ModelCatalog` sınıflarına bağlanıyor.
3. **Burhan: Kendi entity'leri.** Burhan bunları `burhan → develop` PR'ıyla gönderir, Bilal merge eder. Bundan sonra Bilal migration'ı oluşturabilir.
4. **Bilal: Migration.** Bilal migration'ı oluşturup PR'la gönderir, Burhan merge eder. Bundan sonra Burhan `dotnet ef database update` ile kendi veritabanını oluşturur.
5. **Bilal: Haftalık sürüm.** `develop → main` PR'ı merge edilir. Bundan sonra video çekilir.

Şu işler hiçbir adımı beklemez ve diğer işlerle aynı anda yapılabilir: OpenRouter denemesi, ekran taslakları, layout, ana sayfa ve README.

---

## 4. Bilal'in işleri

Burhan, Bilal ilk push'u yapmadan depoyu clone'layamaz. Bu yüzden Bilal işe Adım 1 ve Adım 2 ile başlar.

**Adım 1.** Collaborator davetinin gönderildiğinden ve kabul edildiğinden emin ol. İki üye de git kimlik ayarlarını yapmış olsun (rehberde Bölüm 3).

**Adım 2.** Boş MVC projesini `main` dalına push'la, ardından `develop` ve `bilal` dallarını aç (rehberde Senaryo A). Burhan'a "depo hazır" diye haber ver.

**Adım 3.** PostgreSQL'i Docker'da başlat ve pgAdmin'den bağlan (aşağıda Bölüm 6.1).

**Adım 4.** `bilal` dalında paketleri ekle, `dotnet ef` aracını kur, bağlantı dizesini user-secrets'a kaydet (Bölüm 6.2).

**Adım 5.** `ApplicationDbContext`, `ApplicationUser`, `ModelCatalog` ve `UserApiKey` sınıflarını yaz (Bölüm 6.3). Her sınıf ayrı dosyada olsun. `dotnet build` ile derlendiğini kontrol et.

**Adım 6.** Commit'le, push'la ve `bilal → develop` PR'ı aç. Burhan bu PR merge edilmeden entity'lerine başlayamaz, bu yüzden PR'ı bekletme. PR'ı Burhan merge eder.

**Adım 7.** Burhan'ın entity PR'ı gelince gözden geçir ve "Create a merge commit" ile merge et.

**Adım 8.** Kendi dalında `git pull origin develop` ile Burhan'ın entity'lerini al. Yedi `DbSet` satırının hepsini `ApplicationDbContext` dosyasına ekle.

**Adım 9.** Migration'ı oluştur ve veritabanına uygula (Bölüm 6.4). pgAdmin'de tabloları kontrol et.

**Adım 10.** Migration dosyalarını commit'le, push'la ve PR aç. Burhan merge eder.

**Adım 11.** Tüm PR'lar `develop` dalına girince `develop → main` PR'ını aç (rehberde Senaryo H). Burhan bu PR'ı merge edince videoyu çek, yükle ve Drive paylaşım ayarını kontrol et.

---

## 5. Burhan'ın işleri

Burhan'ın ilk iki işi depoya ihtiyaç duymaz. Burhan bunlara Bilal'i beklemeden başlayabilir.

**Adım 1. OpenRouter denemesi (hemen başlanabilir).**
- openrouter.ai'de hesap aç ve bir API anahtarı oluştur.
- Terminalden streaming açık bir deneme isteği at (Bölüm 6.5).
- Yanıtın son parçalarında `usage` alanının (giriş ve çıkış token sayıları) gelip gelmediğine bak.
- Sonucun ekran görüntüsünü al.
- Anahtarı hiçbir dosyaya, mesaja ya da ekran görüntüsüne koyma.

**Adım 2. Ekran taslakları (hemen başlanabilir).** Giriş, karşılaştırma, sonuç, sıralama ve admin ekranlarını çiz. Kâğıt, Figma veya draw.io fark etmez. Görselleri depodaki `docs/` klasörüne koy.

**Adım 3. Depoyu alma (Bilal'in ilk push'undan sonra).** Daveti kabul et, depoyu clone'la, `develop` dalından kendi `burhan` dalını aç (rehberde Senaryo B).

**Adım 4. Layout ve ana sayfa (Adım 3'ten sonra).** `Views/Shared/_Layout.cshtml` ve `Views/Home/Index.cshtml` dosyalarında çalış. Bootstrap ile mobil uyumlu bir menü ve ana sayfa hazırla. Bu dosyaları başka kimse değiştirmediği için çakışma riski yok. Çalışırken düzenli olarak commit'le ve push'la.

**Adım 5. Entity'ler (Bilal'in DbContext PR'ı merge edildikten sonra).**
- Kendi dalında `git pull origin develop` çalıştır.
- `Comparison`, `ComparisonResult`, `Vote` ve `ModelStat` sınıflarını `Models/` klasöründe, her biri ayrı dosyada olacak şekilde yaz.
- `ApplicationDbContext` dosyasına **dokunma**. `DbSet` satırlarını Bilal ekleyecek.
- `dotnet build` ile derlendiğini kontrol et.
- Rehberdeki Senaryo E'ye göre `burhan → develop` PR'ı aç ve Bilal'e haber ver.

**Adım 6. README iskeleti (sırası fark etmez).** Proje tanımı, kullanılan teknolojiler ve "Kurulum" başlığını yaz.

**Adım 7. Migration sonrası (Bilal'in migration PR'ı merge edildikten sonra).** Kendi dalında `git pull origin develop` çalıştır, ardından **yalnızca** `dotnet ef database update` komutunu çalıştır. Uygulamanın kendi bilgisayarında çalıştığını kontrol et. `migrations add` komutunu asla çalıştırma.

**Adım 8. README'yi tamamla (son iş olarak).** Kurulum, Docker, user-secrets ve migration adımlarını README'ye ekle. PR'la gönder.

---

## 6. Teknik notlar

### 6.1 PostgreSQL (Docker) ve pgAdmin

PostgreSQL'i başlatmak için (her üye kendi bilgisayarında bir kez):

```bash
docker run --name promptlab-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:17
docker ps
```

`docker ps` çıktısında `promptlab-db` görünüyorsa veritabanı çalışıyor demektir. Bilgisayar yeniden başladıktan sonra `docker start promptlab-db` ile tekrar açılır.

pgAdmin'den bağlanmak için:

1. Sol menüde **Servers** üzerine sağ tıkla ve **Register → Server** seç.
2. **General** sekmesinde ad olarak `PromptLab` yaz.
3. **Connection** sekmesinde şu bilgileri gir: host `localhost`, port `5432`, kullanıcı adı `postgres`, şifre `postgres`.

pgAdmin de Docker içinde çalışıyorsa host olarak `host.docker.internal` yaz.

Migration'dan sonra tablolar **Databases → promptlab → Schemas → public → Tables** altında görünür.

### 6.2 Paketler ve user-secrets

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet tool install --global dotnet-ef

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=promptlab;Username=postgres;Password=postgres"
```

Paket sürümleri projenin .NET sürümüyle aynı ana sürümde olmalı. Örneğin proje .NET 10 ise paketler de 10.x olmalı. Gerekirse `--version` ile sürüm belirtilir.

### 6.3 Identity'yi entity'lere şimdiden bağlama

Identity kurulumu 2. haftada yapılacak. Yine de `ApplicationUser` sınıfını şimdiden `IdentityUser` sınıfından türetiyoruz ve DbContext'i `IdentityDbContext` olarak yazıyoruz. Böylece ilk migration Identity tablolarını da içerir ve 2. haftada kullanıcı tablosu baştan değişmez.

```csharp
// Models/ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
}

// Data/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ModelCatalog> ModelCatalogs => Set<ModelCatalog>();
    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();
    // Burhan'ın entity'leri merge edildikten sonra Bilal ekler:
    // Comparisons, ComparisonResults, Votes, ModelStats
}
```

`Program.cs` dosyasına şu kayıt eklenir:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Identity kendi rol tablolarını (`AspNetRoles`, `AspNetUserRoles`) getirdiği için, plan belgesindeki Users tablosunda yer alan "Rol (enum)" alanına gerek yok. Admin ve User rolleri Identity'nin rol sistemiyle yönetilecek.

### 6.4 Migration (yalnızca Bilal)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Burhan, migration PR'ı merge edildikten sonra yalnızca `dotnet ef database update` komutunu çalıştırır.

### 6.5 OpenRouter deneme isteği (Burhan)

Önce anahtarı yalnızca o terminal oturumu için bir ortam değişkenine koy. Böylece komut geçmişinde ve ekran görüntüsünde görünmez:

```bash
export OPENROUTER_API_KEY="anahtarın-buraya"
```

Ardından streaming açık bir istek at. `MODEL_ID` yerine openrouter.ai/models sayfasından bir model kimliği yaz. Ücretsiz modellerin kimliği `:free` ile biter.

```bash
curl -N https://openrouter.ai/api/v1/chat/completions \
  -H "Authorization: Bearer $OPENROUTER_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"model": "MODEL_ID", "messages": [{"role": "user", "content": "Merhaba, kendini bir cümleyle tanıt."}], "stream": true}'
```

- **Çıktının yapısı:** Yanıt `data: {...}` satırları halinde parça parça akar.
- **Kontrol edilecek yer:** Son satırlardan birinde `"usage"` alanı ve içinde token sayıları olmalı.
- **usage gelmezse:** İsteğin JSON gövdesine `"usage": {"include": true}` ekleyip tekrar dene ve sonucu not et.

Windows'ta bu komutu Git Bash'te çalıştırmak en kolayı. Postman da kullanılabilir.

---

## 7. Dikkat edilecekler

- **Herkes kendi hesabıyla:** Herkes kendi hesabıyla commit ve push yapar. Başkasının hesabından yapılan katkı sayılmaz, GitHub puanı kişiseldir. İki üye de bu hafta en az bir commit atmış olmalı.
- **Gizli bilgiler:** Bağlantı dizesi ve OpenRouter anahtarı depoya girmez, user-secrets'ta kalır. Videoda anahtar ya da bağlantı dizesi ekranda görünmemeli.
- **Ayrı dosyalar:** Farklı dosyalarda çalışın. `ApplicationDbContext` dosyasını yalnızca Bilal, `_Layout.cshtml` dosyasını yalnızca Burhan değiştirir.
- **Migration:** Migration'ı yalnızca Bilal oluşturur.
- **Güncelleme:** PR açmadan önce her zaman `git pull origin develop` çalıştırılır.
- **Yetişmeyen işler:** Yetişmeyen bir iş olursa videonun "2. hafta" kısmında söylenir. Eksik işi saklamak yerine hangi hafta yapılacağını söylemek daha iyidir.

---

## 8. Hafta kontrol listesi

- [ ] Depo açık (public), iki üye de collaborator
- [ ] `main`, `develop`, `bilal`, `burhan` dalları GitHub'da var
- [ ] İki üyenin de kendi hesabından commit'i var
- [ ] Uygulama çalışıyor: layout ve ana sayfa, mobilde düzgün görünüyor
- [ ] Yedi tablo (ve Identity tabloları) pgAdmin'de görünüyor
- [ ] OpenRouter denemesi yapıldı, usage alanı kontrol edildi
- [ ] Ekran taslakları `docs/` klasöründe
- [ ] README iskeleti ve kurulum adımları yazıldı
- [ ] `develop → main` PR'ı merge edildi
- [ ] Videoda anahtar ya da bağlantı dizesi görünmüyor
- [ ] Video yüklendi, Drive paylaşım ayarı kontrol edildi
