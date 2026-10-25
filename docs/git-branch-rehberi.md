# PromptLab – Git ve Branch Yönetimi Rehberi

Bu belge, PromptLab deposunda nasıl çalıştığımızı adım adım anlatır. Bir işe başlarken, işi bitirirken ya da bir şey ters gittiğinde ilgili senaryoya bakın ve komutları sırasıyla uygulayın.

Depo adresi: `https://github.com/M4Sherman414/PromptLab.git`

---

## 1. Dal (branch) yapımız

```
main            ← Her haftanın sonunda çalışan, teslim edilen sürüm
 └── develop    ← Ortak çalışma dalı. Biten işler burada birleşir.
      ├── bilal   ← Bilal'in kişisel çalışma dalı
      └── burhan  ← Burhan'ın kişisel çalışma dalı
```

Kurallarımız şunlar:

- **main:** İlk kurulum dışında `main` dalına doğrudan commit atılmaz. `main` yalnızca haftada bir, `develop` dalından gelen Pull Request ile güncellenir.
- **develop:** `develop` dalına da doğrudan commit atılmaz. Biten işler Pull Request ile buraya girer.
- **Kişisel dallar:** Herkes yalnızca kendi dalında (`bilal` veya `burhan`) çalışır. Bu dallar proje boyunca kapatılmaz. Düzenli olarak `develop` ile güncellenir.
- **Merge yöntemi:** Merge işlemlerinde her zaman **"Create a merge commit"** seçeneği kullanılır.

---

## 2. Kısa sözlük

Aşağıdaki terimler belgenin geri kalanında sık geçiyor.

- **Depo (repository):** Projenin tüm dosyalarını ve geçmişini tutan klasör. Bir kopyası GitHub'da, bir kopyası da her birimizin bilgisayarında bulunur.
- **origin:** Bilgisayarındaki deponun GitHub'daki karşılığına verilen kısa ad.
- **Commit:** Yaptığın değişikliklerin bir mesajla birlikte kaydedilmiş anlık görüntüsü. Commit yalnızca kendi bilgisayarına kaydedilir.
- **Push:** Bilgisayarındaki commit'leri GitHub'a göndermek.
- **Pull:** GitHub'daki değişiklikleri bilgisayarına indirip bulunduğun dala birleştirmek.
- **Clone:** GitHub'daki depoyu bilgisayarına ilk kez indirmek. Proje boyunca yalnızca bir kez yapılır.
- **Branch (dal):** Bir commit'ten ayrılan paralel çalışma hattı. Bir dalda yapılan değişiklik diğer dalları etkilemez.
- **Pull Request (PR):** GitHub'da açılan "şu daldaki değişiklikleri şu dala eklemek istiyorum" isteği. Diğer üye inceleyip onaylar.
- **Merge:** İki dalın değişikliklerini birleştirmek.
- **Merge commit:** Merge sırasında oluşan, iki dalı birleştiren özel commit. Bu yöntemde herkesin kendi commit'leri olduğu gibi korunur.
- **Çakışma (conflict):** İki kişinin aynı dosyanın aynı satırını farklı şekilde değiştirmesi. Git hangisinin doğru olduğuna kendisi karar veremez ve size sorar.

---

## 3. Tek seferlik ayarlar (iki üye de yapar)

### 3.1 Git kimliği

```bash
git config --global user.name "Ad Soyad"
git config --global user.email "github-hesabindaki-e-posta@ornek.com"
```

Buradaki e-posta, GitHub hesabına kayıtlı e-postayla **aynı** olmalı. GitHub commit'leri bu e-posta üzerinden hesabına bağlar. Eşleşmezse commit'lerin profilinde görünmez ve katkın sayılmaz.

Ayarın doğru olduğunu kontrol etmek için:

```bash
git config --global user.email
```

### 3.2 Pull ayarı

```bash
git config --global pull.rebase false
```

Bu ayar, `git pull` komutunun değişiklikleri her zaman birleştirme (merge) yoluyla almasını sağlar. Yapılmazsa Git ilk pull'da "divergent branches" uyarısı verip durabilir.

### 3.3 GitHub'a giriş

GitHub, terminalden push yaparken hesap şifresini kabul etmez. En kolay yol GitHub CLI kullanmaktır:

```bash
# Mac
brew install gh
# Windows
winget install GitHub.cli

# Ardından (iki sistemde de)
gh auth login
```

Komut tarayıcıyı açar, giriş yaparsın ve bu kadar. Alternatif olarak GitHub'da Settings, Developer settings, Personal access tokens yolundan "repo" yetkili bir token oluşturup şifre yerine onu girebilirsin.

### 3.4 Collaborator (ortak çalışan) daveti

Deponun sahibi olan hesap, diğer üyeyi deponun **Settings** sayfasında **Collaborators** bölümünden **Add people** ile ekler. Davet edilen kişi daveti GitHub bildiriminden ya da e-postadan kabul eder. Davet kabul edilmeden push yapılamaz ("Permission denied" veya "403" hatası alınır).

### 3.5 (İsteğe bağlı) Yanlış merge türünü engelleme

Depo sahibi, deponun **Settings** sayfasında **General** sekmesine gidip **Pull Requests** bölümünde "Allow squash merging" ve "Allow rebase merging" seçeneklerini kapatabilir. Böylece yalnızca "Create a merge commit" seçeneği kalır ve yanlışlıkla başka yöntem seçilemez.

---

## 4. Senaryo A — İlk kurulum (Bilal, yalnızca bir kez)

**Başlangıç durumu:** MVC projesi `~/Desktop/PromptLab` klasöründe oluşturuldu ve klasör GitHub'daki depoya bağlı. Bağlantıyı `git remote -v` ile kontrol edebilirsin.

**Adım 1.** Proje klasörüne gir:

```bash
cd ~/Desktop/PromptLab
```

**Adım 2.** Commit'lenecek dosyaları kontrol et:

```bash
git status
```

Listede `bin/` ve `obj/` klasörleri **olmamalı**. Varsa `.gitignore` eksik demektir. `dotnet new gitignore` ile ekle.

**Adım 3.** İlk commit'i at ve `main` dalına gönder:

```bash
git add .
git commit -m "Boş MVC projesi"
git push -u origin main
```

`-u`, bilgisayarındaki dalı GitHub'daki dalla eşleştirir. Bundan sonra bu dalda sadece `git push` ve `git pull` yazman yeterli olur.

**Adım 4.** `main` dalından `develop` dalını aç ve GitHub'a gönder:

```bash
git checkout -b develop
git push -u origin develop
```

`git checkout -b develop`, "develop adında yeni bir dal oluştur ve ona geç" demektir. Yeni dal, o an bulunduğun daldan (burada `main`) ayrılır.

**Adım 5.** `develop` dalından kendi kişisel dalını aç ve GitHub'a gönder:

```bash
git checkout -b bilal
git push -u origin bilal
```

Bu komutu `develop` dalındayken çalıştırdığın için `bilal` dalı `develop` dalından ayrılır.

**Adım 6.** Kontrol et:

```bash
git branch -a
```

Çıktıda `main`, `develop` ve `bilal` dallarını, yanlarında da `remotes/origin/...` karşılıklarını görmelisin. Yıldız (*) `bilal` dalının yanında olmalı.

**Adım 7.** Burhan'a "depo hazır, clone'layabilirsin" diye haber ver.

---

## 5. Senaryo B — Burhan'ın projeyi ilk kez alması

**Ön koşul:** Bilal Senaryo A'yı bitirdi ve Burhan collaborator davetini kabul etti.

**Adım 1.** Projeyi koymak istediğin klasöre geç. Örneğin masaüstü:

```bash
cd ~/Desktop
```

Windows'ta bu komut `cd %USERPROFILE%\Desktop` olur.

**Adım 2.** Depoyu indir (clone):

```bash
git clone https://github.com/M4Sherman414/PromptLab.git
cd PromptLab
```

Clone, `PromptLab` adında bir klasör oluşturur, tüm dosyaları ve geçmişi içine indirir ve GitHub bağlantısını (origin) kendisi kurur. Ayrıca `git init` ya da `git remote add` yapmana gerek yok. **Clone proje boyunca yalnızca bir kez yapılır.** Sonraki tüm güncellemeler `git pull` ile alınır.

**Adım 3.** `develop` dalına geç:

```bash
git checkout develop
```

Clone işlemi bilgisayarında yalnızca `main` dalını oluşturur ama GitHub'daki `develop` dalının varlığını bilir. Bu komut, GitHub'daki `develop` dalına bağlı yerel bir `develop` dalı oluşturur.

Komut "did not match any file(s) known to git" hatası verirse, önce GitHub'daki dalların listesini güncelle, sonra tekrar dene:

```bash
git fetch origin
git checkout develop
```

**Adım 4.** `develop` dalından kendi kişisel dalını aç ve GitHub'a gönder:

```bash
git checkout -b burhan
git push -u origin burhan
```

**Adım 5.** Kontrol et:

```bash
git branch
```

Yıldız (*) `burhan` dalının yanında olmalı.

**Adım 6.** Projeyi kendi bilgisayarında çalışır hale getir:

```bash
dotnet restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=promptlab;Username=postgres;Password=postgres"
dotnet run
```

user-secrets her bilgisayarda ayrı tutulur ve depoya girmez. Bu yüzden her üye bağlantı dizesini kendi bilgisayarında bir kez kaydeder.

---

## 6. Senaryo C — Her çalışma oturumunun başı

Her gün çalışmaya başlamadan önce bu adımları uygula. Amaç, diğer üyenin `develop` dalına eklediği değişiklikleri baştan almak ve çakışmaları küçükken yakalamak.

```bash
git checkout <kendi-dalın>      # örn. git checkout bilal
git status                      # "nothing to commit, working tree clean" görmelisin
git pull origin develop         # develop'taki yenilikleri kendi dalına al
git push                        # güncellenmiş dalını GitHub'a gönder
```

`git status` commit'lenmemiş değişiklik gösteriyorsa, önce onları commit'le (Senaryo D), sonra pull yap.

---

## 7. Senaryo D — Çalışırken değişiklikleri kaydetme

Çalışırken küçük ve anlamlı parçalar halinde commit at. Mesela bir entity bittiğinde ya da bir sayfa çalışır hale geldiğinde.

```bash
git add .
git commit -m "ModelCatalog entity'si eklendi"
git push
```

Commit mesajı ne yapıldığını kısa ve net söylemeli. "düzeltme", "asdf" ya da "son hali" gibi mesajlar sonradan hiçbir şey anlatmaz. Örnekler:

- `Comparison ve ComparisonResult entity'leri eklendi`
- `Layout'a Bootstrap navbar eklendi`
- `README'ye kurulum adımları yazıldı`

Push'u sık yapmak, bilgisayarına bir şey olursa işin kaybolmamasını sağlar ve diğer üyenin ne yaptığını görmesine yardım eder.

---

## 8. Senaryo E — İş bitti, develop dalına gönderme

Bu senaryo, kendi işin bittiğinde ve bu arada diğer üye `develop` dalına yeni şeyler eklemiş olabileceğinde uygulanır. Kural basit: **PR açmadan hemen önce her zaman `develop` dalını kendi dalına al.**

**Adım 1.** Son değişikliklerini commit'le:

```bash
git add .
git commit -m "Yapılan işin kısa açıklaması"
```

**Adım 2.** `develop` dalının güncel halini kendi dalına al:

```bash
git pull origin develop
```

Çakışma çıkarsa önce Senaryo G'yi uygula, sonra buradan devam et.

**Adım 3.** Projenin birlikte çalıştığını test et:

```bash
dotnet build
dotnet run
```

İki kişinin kodu bir araya geldiğinde derleme hatası çıkabilir. Bunu PR'dan önce kendi bilgisayarında görmek, `develop` dalını bozmaktan çok daha iyidir.

**Adım 4.** Dalını GitHub'a gönder:

```bash
git push
```

**Adım 5.** GitHub'da Pull Request aç:

1. Depo sayfasında **Pull requests** sekmesine geç, **New pull request** düğmesine bas.
2. Üstte iki açılır menü göreceksin. **base** olarak `develop`, **compare** olarak kendi dalını (`bilal` veya `burhan`) seç. Ok yönü "kendi dalın → develop" şeklinde olmalı.
3. Aşağıda değişen dosyaları gözden geçir ve **Create pull request** düğmesine bas.
4. Başlığa ne yaptığını yaz (örn. "Comparison entity'leri"). Açıklamaya kısa bir not ekle ve tekrar **Create pull request** düğmesine bas.
5. Diğer üyeye "PR açtım" diye haber ver.

**Adım 6.** Diğer üye PR'ı merge eder:

1. PR sayfasında **Files changed** sekmesinden değişikliklere göz atar.
2. Sayfanın altındaki yeşil düğmenin sağındaki küçük oka (▾) basar ve **Create a merge commit** seçeneğinin seçili olduğundan emin olur. Seçiliyse düğmede "Merge pull request" yazar.
3. **Merge pull request** düğmesine, ardından **Confirm merge** düğmesine basar.

**Adım 7.** Merge'den sonra **iki üye de** kendi dalında `develop` dalını alır:

```bash
git checkout <kendi-dalın>
git pull origin develop
git push
```

PR açan kişinin de bunu yapması gerekir, çünkü merge işlemi `develop` dalına yeni bir merge commit'i ekledi ve kendi dalın da bunu almalı.

**PR açıkken yeni commit:** PR açıkken kendi dalına yeni commit'ler push'larsan, bunlar o PR'a otomatik olarak eklenir. Yeni PR açmana gerek yok.

---

## 9. Senaryo F — Diğer üyenin bitirdiği işi alma

Diğer üye "PR'ım merge edildi" dediğinde, onun yaptıklarını kendi dalına almak için:

```bash
git checkout <kendi-dalın>
git status                  # yarım işin varsa önce commit'le
git pull origin develop
git push
```

Bu işlem PR açmayı gerektirmez. Sadece kendi dalını güncel tutar. PR'ı ancak kendi işin bittiğinde açarsın (Senaryo E).

---

## 10. Senaryo G — Çakışma çözme

`git pull origin develop` sonrasında şuna benzer bir mesaj görürsen çakışma var demektir:

```
CONFLICT (content): Merge conflict in Views/Shared/_Layout.cshtml
Automatic merge failed; fix conflicts and then commit the result.
```

Git, çakışan dosyanın içine iki versiyonu birden yazar:

```
<<<<<<< HEAD
Senin dalındaki hali
=======
develop'tan gelen hali
>>>>>>> develop
```

**Adım 1.** Çakışan dosyaları gör:

```bash
git status
```

"both modified" yazan dosyalar çakışmalı dosyalardır.

**Adım 2.** Her dosyayı VS Code'da aç. Çakışan bölümün üstünde dört seçenek çıkar:

- **Accept Current Change:** Senin dalındaki hali tut.
- **Accept Incoming Change:** `develop` dalından gelen hali tut.
- **Accept Both Changes:** İkisini alt alta tut.
- **Compare Changes:** İki hali yan yana karşılaştır.

Doğru olanı seç. Gerekirse elle düzenle. `<<<<<<<`, `=======` ve `>>>>>>>` satırlarının hiçbiri dosyada kalmamalı. Emin olamazsan diğer üyeye sor. Yanlış tarafı seçmek onun kodunu silebilir.

**Adım 3.** Dosyaları kaydet ve birleştirmeyi tamamla:

```bash
git add .
git commit -m "develop ile birleştirme, çakışmalar çözüldü"
```

**Adım 4.** `dotnet build` ile projenin derlendiğini kontrol et, sonra `git push` yap.

**Vazgeçmek istersen:** Çakışmayı çözmekten vazgeçip pull öncesine dönmek için şu komutu çalıştır:

```bash
git merge --abort
```

---

## 11. Senaryo H — Haftalık sürüm: develop dalından main dalına

**Ne zaman:** Her haftanın sonunda, video çekilmeden önce.
**Kim:** O haftanın lideri PR'ı açar, diğer üye merge eder.

1. Önce iki üyenin de PR'larının `develop` dalına merge edildiğinden emin olun.
2. GitHub'da **Pull requests**, ardından **New pull request** düğmesine basın.
3. **base:** `main`, **compare:** `develop`. Ok yönü "develop → main" şeklinde olmalı.
4. Başlık: "1. hafta sürümü" (her hafta numarasını değiştirin). **Create pull request** düğmesine basın.
5. Diğer üye **Create a merge commit** seçeneğiyle merge eder.

GitHub katkı grafiği yalnızca varsayılan dal olan `main` dalına giren commit'leri sayar. Bu adım atlanırsa o haftanın commit'leri profillerde görünmez.

---

## 12. Migration kuralı

Migration, entity sınıflarındaki değişiklikleri veritabanına uygulayan, EF Core tarafından otomatik oluşturulan dosyadır. Bu dosyaların çakışması zor çözülür. O yüzden:

- **Kim oluşturur:** `dotnet ef migrations add ...` komutunu **yalnızca bir kişi** çalıştırır (1. haftada Bilal). Oluşan dosyalar normal bir iş gibi PR ile `develop` dalına gönderilir.
- **Diğer üye ne yapar:** `develop` dalını kendi dalına aldıktan sonra **yalnızca** şu komutu çalıştırır. Bu komut yeni migration oluşturmaz, var olanları kendi veritabanına uygular:

```bash
dotnet ef database update
```

- **Entity değişikliği:** Bir entity'de değişiklik gerektiğinde, kodu kim yazarsa yazsın, migration'ı oluşturacak kişiye haber verilir. İki kişi aynı anda migration oluşturmaz.

---

## 13. Altın kurallar

1. **Clone tek sefer.** Clone proje boyunca bir kez yapılır. Sonraki tüm güncellemeler `git pull` ile alınır.
2. **Önce dalını kontrol et.** Çalışmaya başlamadan önce `git branch` ile hangi dalda olduğuna bak. `main` veya `develop` dalında çalışma.
3. **Her oturum başında güncelle.** Her çalışma oturumunun başında `git pull origin develop` çalıştır.
4. **PR'dan hemen önce de güncelle.** PR açmadan hemen önce yine `git pull origin develop` çalıştır.
5. **Doğru merge yöntemi.** Merge her zaman "Create a merge commit" ile yapılır.
6. **Herkes kendi hesabıyla.** Herkes kendi hesabıyla commit ve push yapar. Başkasının bilgisayarından yapılan commit o kişiye sayılmaz.
7. **Gizli bilgi depoya girmez.** Bağlantı dizesi ve API anahtarları user-secrets'ta kalır. `appsettings.json` dosyasına yazılmaz.
8. **Tek kişi migration.** Migration yalnızca bir kişi tarafından oluşturulur.
9. **Dosya paylaşımı.** Mümkün olduğunca farklı dosyalarda çalışın. Her sınıf ayrı dosyada olsun.

---

## 14. Sorun giderme

**Push "rejected" veya "fetch first" hatası veriyor.**
GitHub'daki dalda senin bilgisayarında olmayan commit'ler var. Önce onları al, sonra tekrar push'la:

```bash
git pull origin <dal-adı>
git push
```

**"Permission denied" veya "403" hatası.**
Collaborator daveti kabul edilmemiş ya da yanlış hesapla giriş yapılmış. Daveti kontrol et, gerekirse `gh auth login` ile tekrar giriş yap.

**Push sırasında şifre kabul edilmiyor.**
GitHub hesap şifresini kabul etmez. Bölüm 3.3'teki `gh auth login` yöntemini ya da personal access token kullan.

**"divergent branches" uyarısı.**
Bölüm 3.2'deki `git config --global pull.rebase false` ayarını yap ve pull'u tekrarla.

**Yanlış dalda değişiklik yaptım, henüz commit'lemedim.**
Değişiklikleri geçici olarak kenara koy, doğru dala geç ve geri getir:

```bash
git stash
git checkout <doğru-dal>
git stash pop
```

**Hangi daldayım, ne değişti, son commit'ler neler?**

```bash
git branch            # bulunduğun dal (* işaretli)
git status            # değişen dosyalar
git log --oneline -10 # son 10 commit
```

---

## 15. Komut özeti

| Ne yapmak istiyorum? | Komut |
|---|---|
| Depoyu ilk kez indirmek | `git clone https://github.com/M4Sherman414/PromptLab.git` |
| Hangi daldayım? | `git branch` |
| Kendi dalıma geçmek | `git checkout bilal` / `git checkout burhan` |
| Yeni dal açıp ona geçmek | `git checkout -b dal-adi` |
| Neler değişti? | `git status` |
| Değişiklikleri kaydetmek | `git add .` sonra `git commit -m "mesaj"` |
| GitHub'a göndermek | `git push` |
| develop'taki yenilikleri almak | `git pull origin develop` |
| GitHub'daki dal listesini yenilemek | `git fetch origin` |
| Çakışmalı birleştirmeden vazgeçmek | `git merge --abort` |
| Commit'lenmemiş işi geçici saklamak | `git stash` / geri almak: `git stash pop` |
| Son commit'leri görmek | `git log --oneline -10` |
