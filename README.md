# Tryeza — Food Sales API

**Backend për shitjen e produkteve ushqimore, porositë, pagesat dhe raportimin financiar.**

Ndërtuar me ASP.NET Core 10, Entity Framework Core 10 dhe SQL Server. API-ja shërben ndërfaqen React/PWA nga i njëjti origin pas paketimit dhe mban të ndara shitjet, arkëtimet dhe borxhet.

[Frontend-i](https://github.com/adrianmehaj/food-sales-web) · [Arkitektura](docs/ARCHITECTURE.sq.md) · [Licenca](LICENSE)

## Funksionaliteti

- Katalog publik, produkte me njësi kg/litër/copë, stok dhe oferta me afat.
- Porosi online, edhe pa llogari, dhe regjistrime manuale nga administratori.
- Dërgesë në adresë me datë të dëshiruar ose marrje vetë.
- Peshë përfundimtare për produktet me peshë dhe rillogaritje e totalit.
- Pagesa të plota, të pjesshme ose të mëvonshme, me historik dhe miratim borxhi.
- Klientë individë/biznese, çmime të veçanta dhe afate pagese.
- Profile personale, role administrimi dhe qasje vetëm te të dhënat përkatëse.
- Banera me galeri, fotografi të ngarkuara nga pajisja dhe njoftime brenda platformës.
- Pasqyrë financiare sipas periudhës dhe raporte të faqëzuara për secilin klient.

## Teknologjitë dhe arkitektura

| Shtresa                    | Përgjegjësia                                                           |
| -------------------------- | ---------------------------------------------------------------------- |
| `FoodSales.Api`            | HTTP, controller-at, autorizimi, CSRF, trajtimi i gabimeve dhe Swagger |
| `FoodSales.Application`    | Kontratat, ndërfaqet dhe rrjedhat e përdorimit                         |
| `FoodSales.Domain`         | Entitetet dhe rregullat e biznesit                                     |
| `FoodSales.Infrastructure` | EF Core, SQL Server, Identity, ruajtja e skedarëve dhe raportet        |

```text
src/
  FoodSales.Api/
  FoodSales.Application/
  FoodSales.Domain/
  FoodSales.Infrastructure/
    Persistence/Migrations/
scripts/                 Nisja, administratori fillestar dhe publikimi
docs/                    Dokumentimi i arkitekturës
FoodSales.sln
```

Çdo controller ka skedarin e vet. Produktet e një porosie lexohen me një kërkesë SQL të kufizuar nga lista e artikujve; çmimet e veçanta të biznesit lexohen gjithashtu në grup. Përfundimi i shitjes ruan gjurmimin EF dhe kontrollin e versionit të stokut.

Controller-at nuk kthejnë entitete EF dhe nuk përdorin drejtpërdrejt kontekstin e databazës. Varësitë dhe rregullat e zgjerimit dokumentohen te [arkitektura](docs/ARCHITECTURE.sq.md).

## Nisja lokale

### Parakushtet

- .NET SDK 10.
- SQL Server funksional; konfigurimi lokal përdor **SQL Server LocalDB në Windows**.
- Node.js 22.12+ vetëm nëse ndërtoni edhe frontend-in.
- Për Visual Studio: version që mbështet .NET 10 dhe workload **ASP.NET and web development**.

Nga dosja e backend-it:

```powershell
dotnet restore
dotnet tool restore
dotnet ef database update --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
dotnet run --project src/FoodSales.Api --launch-profile FoodSales.Api
```

| Adresa lokale                              | Qëllimi                                               |
| ------------------------------------------ | ----------------------------------------------------- |
| `http://localhost:5080`                    | API-ja dhe frontend-i, nëse është paketuar            |
| `http://localhost:5080/swagger/index.html` | Dokumentimi interaktiv në Development                 |
| `http://localhost:5080/health`             | Kontrolli i procesit; nuk provon lidhjen me databazën |

Lidhja e parazgjedhur lokale përdor `(localdb)\MSSQLLocalDB`, databazën `FoodSales` dhe Windows Authentication. Për një instancë tjetër, vendosni `ConnectionStrings__Shop` përmes konfigurimit lokal, user secrets ose environment variables. Kredencialet reale nuk duhen ruajtur në repository.

Produkte ilustruese mund të shtohen veçmas:

```powershell
dotnet run --project src/FoodSales.Api -- --seed-demo
```

Kjo komandë lejohet vetëm në Development dhe katalog bosh; nuk krijon klientë, pagesa ose shitje.

### Visual Studio

Hapni `FoodSales.sln`, vendosni **FoodSales.Api** si **Startup Project**, zgjidhni profilin `FoodSales.Api` dhe nisni me F5. Për frontend-in gjatë zhvillimit përdorni `npm run dev` në checkout-in e veçantë. Nisni vetëm një backend në portën 5080.

## Migrimet

### Package Manager Console në Visual Studio

Zgjidhni `FoodSales.Infrastructure` te **Default project** dhe `FoodSales.Api` si startup project:

```powershell
Add-Migration "PershkrimiINdryshimit"
Update-Database
```

`Add-Migration` përdoret vetëm kur ndryshon skema; për një checkout ekzistues mjafton aplikimi i migrimeve me `Update-Database`.

### Terminali

```powershell
dotnet ef migrations add PershkrimiINdryshimit --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
```

`dotnet-ef` përcaktohet në `.config/dotnet-tools.json`. Factory i kontekstit lexon konfigurimin e API-së dhe lejon migrime pa nisur serverin HTTP. Ndaloni debug-un nëse Windows raporton DLL të bllokuara.

## Administratori fillestar

Nuk ka llogari ose fjalëkalime të paracaktuara. Në PowerShell:

```powershell
.\scripts\Create-Admin.ps1 -Email 'admin@example.com'
```

Skripti kërkon fjalëkalimin privatisht. Nëse llogaria ekziston si klient dhe ende nuk ka administrator:

```powershell
.\scripts\Grant-Admin.ps1 -Email 'admin@example.com'
```

Komandat fillestare refuzohen pasi krijohet administratori i parë. Administratorët e tjerë shtohen nga paneli **Administratorët**, brenda një llogarie me rol Admin. Regjistrimi publik krijon vetëm llogari klienti.

## Autentikimi dhe API-ja

Përdoret **ASP.NET Core Identity me cookie sesioni**, jo Bearer JWT. Cookie-t janë HttpOnly; veprimet që ndryshojnë të dhëna kërkojnë token CSRF.

1. Merrni tokenin nga `GET /api/account/csrf` duke ruajtur cookie-t.
2. Dërgoni tokenin në header-in `X-CSRF-TOKEN` për POST/PUT/PATCH/DELETE.
3. Hyni përmes `POST /api/account/login`; përdorni të njëjtin sesion për kërkesat pasuese.

Swagger ofrohet vetëm në Development dhe përfshin konfigurimin lokal për cookie/CSRF. Përgjigjet private të API-së nuk ruhen në cache.

| Rrugët kryesore                                                    | Përdorimi                                         |
| ------------------------------------------------------------------ | ------------------------------------------------- |
| `/api/catalog/*`                                                   | Produkte, oferta, banera dhe të dhënat e biznesit |
| `/api/account/*`                                                   | Sesioni dhe llogaria                              |
| `/api/orders` dhe `/api/orders/mine`                               | Porosia dhe historiku personal                    |
| `/api/admin/products`, `/api/admin/orders`, `/api/admin/customers` | Menaxhimi i shitjeve dhe katalogut                |
| `/api/admin/dashboard` dhe `/api/admin/reports/customers`          | Pasqyra dhe raportet financiare                   |
| `/api/admin/administrators`                                        | Menaxhimi i administratorëve                      |
| `/api/notifications`                                               | Njoftimet e dukshme për përdoruesin e hyrë        |
| `/api/admin/images` dhe `/media/{fileName}`                        | Ngarkimi dhe shfaqja e fotografive                |

Për kontratat, parametrat dhe metodat HTTP shihni Swagger-in lokal.

## Rregullat financiare

- Vetëm porosia **Completed** llogaritet si shitje. Shitjet raportohen sipas përfundimit, pagesat sipas datës së arkëtimit.
- Pagesa e mëvonshme zvogëlon borxhin pa krijuar shitje të dytë. Pagesa mbi borxhin refuzohet.
- Stoku zbritet në përfundim të shitjes, brenda transaksionit. Porositë në pritje nuk rezervojnë stok.
- Pesha reale ndryshon totalin para përfundimit. Sasitë mbështesin deri në 3 shifra dhjetore; copët janë të plota dhe paratë kanë 2 shifra.
- Borxhi i mbetur në përfundim kërkon miratimin e administratorit. Statusi i pagesës është i ndarë nga statusi i porosisë.
- Çmimi i posaçëm i biznesit ka përparësi ndaj ofertës publike; çmimet ruhen si snapshot i porosisë.
- Idempotenca, rowversion dhe transaksionet mbrojnë nga riprovimet dhe ndryshimet konkurrente.
- Telefoni nuk provon pronësinë: porositë pa llogari nuk lidhen automatikisht me një llogari të regjistruar.

Shembull: shitje **20 €**, pagesë fillestare **12 €**, borxh **8 €**. Pagesa e mëvonshme prej 8 € mbyll borxhin; shitja mbetet 20 €.

## Fotografitë dhe konfigurimi

Fotografitë pranojnë JPG/JPEG/JFIF, PNG dhe WebP deri në 5 MB sipas konfigurimit. Backend-i kontrollon madhësinë, prapashtesën, MIME-in dhe nënshkrimin e përmbajtjes; emrin dhe prapashtesën e ruajtur i gjeneron vetë sipas formatit të njohur.

| Konfigurimi              | Qëllimi                                              |
| ------------------------ | ---------------------------------------------------- |
| `ConnectionStrings:Shop` | Lidhja me SQL Server                                 |
| `Business`               | Emri, qyteti, telefoni dhe të dhënat e marrjes       |
| `ImageUploads:RootPath`  | Dosja e fotografive; parazgjedhje `App_Data/uploads` |
| `ImageUploads:MaxBytes`  | Madhësia maksimale e fotografisë                     |
| `Pagination`             | Madhësia e parazgjedhur dhe kufiri i faqeve          |

Skedarët ruhen jashtë `wwwroot`; SQL ruan rrugën `/media/...`. Dosja e fotografive duhet të jetë e qëndrueshme në hostim dhe pjesë e kopjes rezervë. Heqja e fotografisë nga formulari heq lidhjen, jo skedarin fizik.

## Publikimi si një aplikacion

Me dy checkout-e, p.sh. në dosje fqinje, nga backend-i:

```powershell
.\scripts\Publish-App.ps1 -FrontendPath ../food-sales-web
```

Skripti instalon varësitë e frontend-it, ndërton React/PWA, e kopjon në `wwwroot` dhe publikon aplikacionin në `artifacts/publish`. Mund të jepen parametrat `-OutputPath` dhe `-Dotnet`.

Frontend-i dhe API-ja mund të shërbehen nga e njëjta adresë; SQL Server mbetet shërbim i veçantë. Për prodhim konfiguroni:

- HTTPS, reverse proxy sipas hostit dhe lidhjen SQL jashtë kodit, me certifikatë të verifikuar.
- Ruajtje të qëndrueshme për fotografitë dhe çelësat ASP.NET Core Data Protection.
- Kopje rezervë të databazës dhe fotografive, si dhe aplikim të kontrolluar të migrimeve.
- Zonën kohore të biznesit: projekti përdor kohën lokale për datat dhe raportet.

LocalDB dhe Vite development server nuk janë konfigurim prodhimi. Ngrok shërben për testim të instalimit lokal, jo si hostim i qëndrueshëm. Kodi nuk përfshin llogari ose burime cloud të krijuara automatikisht.

## Verifikimi dhe kufizimet

```powershell
dotnet build FoodSales.sln
dotnet ef migrations list --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
```

Workflow-i [Backend CI](.github/workflows/ci.yml) bën restore, kontroll të formatimit dhe build Release për push në `main` dhe pull request. Ai nuk lidh SQL Server dhe nuk zbaton migrime. `.editorconfig` përcakton formatimin C#.

Projekti nuk përmban ende një suitë të automatizuar testimi regresiv. Kontrollet CI verifikojnë formatimin dhe build-in; rrjedhat e biznesit dhe instalimi PWA kontrollohen veçmas.

Ende nuk përfshihen rikthimi i fjalëkalimit, verifikimi i emailit, MFA, rimbursimet dhe njoftimet email/SMS/push jashtë platformës. Njoftimet aktuale janë brenda aplikacionit. Eksporti PDF/Excel realizohet në frontend, me kufi 5,000 rreshta për listë.

## Licenca

Kodi shpërndahet sipas [licencës MIT](LICENSE). Varësitë ruajnë licencat e tyre përkatëse.
