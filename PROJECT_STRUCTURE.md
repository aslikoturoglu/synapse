# Proje Yapısı

Synapse, uçtan uca %100 .NET/C# ile yazılan bir web uygulaması. İki ayrı .NET
projesinden oluşur; birbirleriyle sadece HTTP üzerinden konuşurlar, kod
paylaşmazlar.

```
synapse/
├── Synapse.slnx        # Her iki projeyi tek çatı altında toplayan çözüm dosyası
├── dotnet-tools.json    # Proje bazlı CLI araçları (dotnet-ef)
├── server/              # ASP.NET Core Web API — Client.csproj değil, Server.csproj
└── client/              # Blazor WebAssembly — tarayıcıda çalışan bağımsız uygulama
```

## Neden iki ayrı proje?

- **server/** veriyi ve iş mantığını yönetir: veritabanına erişir, kuralları
  uygular, dışarıya sadece API (HTTP endpoint) olarak açar.
- **client/** kullanıcının tarayıcısında çalışır: ekranları çizer, kullanıcı
  girdisini alır, ihtiyaç duyduğu veriyi server'dan HTTP ile ister.
- İkisi ayrı süreç, ayrı porttur (server: 5057, client: 5296). Biri olmadan
  diğeri de derlenip çalışabilir — aralarındaki tek bağ, client'ın
  `wwwroot/appsettings.json` içindeki `ApiBaseUrl` ile server'ın adresini
  bilmesi ve server'ın CORS ile client'ın adresine izin vermesidir.

Kural: **Blazor Server / SSR kullanılmaz.** UI her zaman tarayıcıda
(WebAssembly) çalışmalı — yoksa "client" ismi anlamını yitirir, mantıken
server'ın bir parçası olur.

## server/ — ASP.NET Core Web API

Katmanlı mimari uygulanır: **Controller → Service → Data**. Bir katman bir
alt katmanı çağırır, üstünü atlayıp geçmez.

| Dosya/Klasör | Amaç |
|---|---|
| `Server.csproj` | Proje tanımı: hedef framework (net10.0), paket referansları |
| `Program.cs` | Giriş noktası; servisleri (Controllers, EF Core, CORS) kaydeder, HTTP pipeline'ı kurar |
| `Controllers/` | Dış dünyadan gelen HTTP isteklerini karşılayan uç noktalar |
| `Services/` | İş mantığı — Controller'ın çağırdığı, kararların verildiği katman |
| `Data/` | `AppDbContext` — Entity Framework Core'un veritabanına açılan kapısı |
| `Models/` | Veritabanı entity'leri ve/veya DTO'lar |
| `Migrations/` | EF Core'un ürettiği veritabanı şema geçmişi (elle düzenlenmez) |
| `appsettings.json` / `appsettings.Development.json` | Ortam bazlı yapılandırma (CORS izinli origin'ler burada, DB şifresi burada **değil** — `dotnet user-secrets` içinde) |
| `Properties/launchSettings.json` | `dotnet run` ile hangi portta/ortamda çalışacağı |
| `Server.http` | Endpoint'leri elle test etmek için örnek istek dosyası |

## client/ — Blazor WebAssembly

| Dosya/Klasör | Amaç |
|---|---|
| `Client.csproj` | Proje tanımı (Blazor WebAssembly Standalone App şablonu) |
| `Program.cs` | WASM giriş noktası; `App` bileşenini sayfaya bağlar, server'a istek atacak `HttpClient`'ı kurar |
| `App.razor` | Sayfalar arası yönlendirme (routing) kökü — bkz. aşağıdaki ayrıntılı açıklama |
| `_Imports.razor` | Her `.razor` dosyasında otomatik geçerli `@using` ifadeleri |
| `Layout/MainLayout.razor` | Tüm sayfaları saran ortak iskelet (header, wrapper vb.) |
| `Pages/` | Her route için bir sayfa (`Home.razor` → `/`, `NotFound.razor` → bilinmeyen route) |
| `Components/` | Sayfalar arası tekrar kullanılan küçük parçalar (Button, Card, Navbar vb.) — henüz boş |
| `wwwroot/` | Tarayıcının doğrudan erişebildiği statik dosyalar — bkz. aşağıdaki ayrıntılı açıklama |
| `Properties/launchSettings.json` | Client'ın dev sunucusunun hangi portta çalışacağı (5296) |

## `wwwroot/` nedir?

`wwwroot`, ASP.NET Core / Blazor projelerinde **tarayıcının doğrudan HTTP ile
isteyebileceği statik dosyaların** tutulduğu klasördür. Bu klasörün dışındaki
hiçbir dosyaya (örn. `Program.cs`, `.razor` dosyaları) tarayıcı doğrudan
erişemez — onlar derlenip WebAssembly paketinin içine gömülür.

`wwwroot` içinde tipik olarak şunlar bulunur:
- **`index.html`** — tarayıcının yüklediği **tek gerçek HTML sayfası**. Blazor
  WASM bir SPA (Single Page Application) olduğu için, kullanıcı hangi route'a
  giderse gitsin (`/login`, `/profile` vb.) sunucudan hep bu aynı `index.html`
  döner; hangi Razor bileşeninin gösterileceğine tarayıcıda çalışan
  JavaScript/WASM motoru (`_framework/blazor.webassembly.js`) karar verir.
- **CSS/resim/font dosyaları** (`css/app.css`, `icon-192.png` gibi) — direkt
  URL ile erişilebilen statik varlıklar.
- **`appsettings.json`** — client'a özel yapılandırma (bizde `ApiBaseUrl`);
  WASM açılırken bu dosyayı bir HTTP isteğiyle indirip okur.
- **`_framework/`** — build sırasında otomatik üretilen klasör (derlenmiş
  WASM/DLL dosyaları); kaynak kodda elle oluşturulmaz, `bin/` gibi düşünülebilir.

Yani `wwwroot` = "bu klasörün içindekiler ham haliyle internete açık", geri
kalan her şey ise derlenip paketlenerek dolaylı yoldan sunulur.

## `App.razor` tam olarak nedir?

`App.razor`, sayfa içeriklerinin (Home, Login, NotFound vb.) render edileceği
**ortak kök bileşendir** — ama kendisi bir "sayfa" değil, bir **yönlendirici
(router) kurulumudur**:

```razor
<Router AppAssembly="@typeof(App).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)"/>
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
</Router>
```

Adım adım:
1. **`<Router>`** — mevcut URL'e (`/`, `/login` vb.) bakıp, `AppAssembly`
   içinde `@page "..."` direktifiyle o route'u karşılayan bileşeni bulur.
   (`AppAssembly` = "route taraması için hangi derlemeye bakılacak", burada
   client'ın kendi derlemesi.)
2. **`<Found>`** — eşleşen bir sayfa bulunduysa, o sayfayı
   **`<RouteView>`** ile, `DefaultLayout` olarak belirtilen `MainLayout`
   içine sararak ekrana basar.
3. Eşleşme bulunamazsa (`NotFoundPage`) `Pages/NotFound.razor` gösterilir.

Yani **evet, sayfalar arasında gezinmeyi yöneten kök odur** — ama içeriği
kendisi üretmez, sadece "hangi sayfa, hangi layout içinde gösterilecek"
kararını verip devrederi. Klasik bir web sitesindeki her sayfanın ayrı HTML
dosyası olmasının Blazor'daki karşılığı budur; farkı, sayfa geçişlerinin
tarayıcıyı yeniden yüklemeden (client-side routing) gerçekleşmesidir.

## Şu an kullanılmayan / büyük ihtimalle proje geliştikçe de kullanılmayacak dosyalar

- **`server/Controllers/WeatherForecastController.cs`** ve
  **`server/WeatherForecast.cs`** — `dotnet new webapi` şablonunun getirdiği
  demo kod. Projenin gerçek amacıyla (AI/sinaps/döküman analizi) hiçbir
  ilgisi yok, ilk gerçek controller yazılınca silinmeli.
- **`server/Server.http`** — sadece yukarıdaki demo endpoint'i test ediyor;
  gerçek endpoint'ler geldikçe ya güncellenmeli ya da silinmeli.
- **`client/wwwroot/icon-192.png`** — Blazor WASM şablonunun getirdiği
  favicon/PWA ikonu. `index.html`'de hiçbir yerden referans verilmiyor
  (proje PWA olarak yapılandırılmadı), şu an tamamen "yetim" bir dosya.
- **`server/Models/.gitkeep`, `server/Services/.gitkeep`,
  `client/Components/.gitkeep`** — bunlar gerçek kod değil, sadece git boş
  klasörleri takip edemediği için konan yer tutuculardır. İlk gerçek dosya o
  klasöre eklendiğinde bu `.gitkeep` dosyaları silinmeli.
