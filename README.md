# Food Sales API — Tryeza

ASP.NET Core 10, EF Core 10 dhe SQL Server. Clean Architecture: Api, Application, Domain, Infrastructure. React/PWA publikohet brenda wwwroot të ASP.NET Core, në të njëjtën adresë. SQL Server mbetet databazë më vete. Emri i instalimit PWA është Tryeza - Produkte Bio.

## Visual Studio dhe VS Code

Hapni `FoodSales.sln` në Visual Studio 2026 të përditësuar, me workload **ASP.NET and web development** dhe SDK .NET 10. Zgjidhni `FoodSales.Api` → **Set as Startup Project**, profilin `FoodSales.Api`, pastaj F5. API nis në http://localhost:5080 dhe profili hap automatikisht Swagger në http://localhost:5080/swagger. Visual Studio 2022 (17.11.5 i gjetur në këtë kompjuter) nuk mbështet target-in net10.0; skripti Start-App.ps1 punon me SDK-në lokale pa ndryshuar Visual Studio.

Në VS Code hapni dosjen `food-sales-web`, pastaj terminalin `npm run dev`; frontend-i është në http://127.0.0.1:5173 dhe lidhet me API-në në portin 5080. Nëse po përdorni dy terminale gjatë zhvillimit, mos nisni edhe një kopje tjetër backend-i në të njëjtin port. Publikimi i prodhimit mbetet një aplikacion i vetëm.

## Migrimet nga Visual Studio

Në terminalin e Visual Studio-s, nga dosja ku është `FoodSales.sln`, me SDK .NET 10 në PATH:

```powershell
dotnet tool restore
dotnet ef migrations list --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
dotnet ef database update --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
```

`dotnet-ef` është përcaktuar në `.config/dotnet-tools.json` me versionin 10.0.14, në përputhje me EF Core të projektit. Për një ndryshim të ri real në model, krijoni migrim me emër përshkrues:

```powershell
dotnet ef migrations add EmriPershkrues --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api --output-dir Persistence/Migrations
```

Në **Tools → NuGet Package Manager → Package Manager Console**, mbështeten gjithashtu komandat PMC sepse API ka paketën Microsoft.EntityFrameworkCore.Tools:

```powershell
Get-Migration -Project FoodSales.Infrastructure -StartupProject FoodSales.Api
Update-Database -Project FoodSales.Infrastructure -StartupProject FoodSales.Api
Add-Migration EmriPershkrues -Project FoodSales.Infrastructure -StartupProject FoodSales.Api -OutputDir Persistence/Migrations
```

Komandat Add-Migration janë shembuj për ndryshime të reja; nuk ka nevojë të krijoni migrim bosh tani. Migrimet ekzistuese janë aplikuar. Ndaloni aplikacionin para krijimit të migrimit nëse Windows raporton skedarë build të bllokuar. Connection string merret nga API/config/user secrets/environment.

## Swagger

Swagger regjistrohet nga `AddApiServices` dhe aktivizohet nga `app.UseDevelopmentSwagger()` në Program.cs vetëm për Development. Dokumenti është `/swagger/v1/swagger.json`; UI është `/swagger`. Endpoint-et dhe asetet u kontrolluan me HTTP 200. Hapja automatike në F5 është konfiguruar, por ende nuk është verifikuar duke kontrolluar vetë Visual Studio-n.

Për veprimet e mbrojtura përdorni `POST /api/account/login` në Swagger, pastaj endpoint-in e dëshiruar. Cookie e sesionit përdoret në të njëjtën adresë; initializer-i lokal merr token të ri antiforgery për çdo POST/PUT/PATCH/DELETE. Autorizimi Admin dhe kontrollet e pronësisë mbeten aktive. Swagger nuk ruan fjalëkalime/tokene në localStorage dhe nuk kërkon përjashtim nga CSP-ja e aplikacionit.

## Nisja lokale

Kërkohen .NET SDK 10, Node 22.12+ dhe instancë SQL funksionale. SDK 10 u instalua lokalisht edhe në `C:\Users\Administrator\Documents\ChatGPT\Food-sales-app\.dotnet`. Shtojeni këtë dosje në PATH nëse terminali ende përdor SDK 8.

**LocalDB tani lidhet dhe migrimet u aplikuan në FoodSales.** Rrjedhat kryesore u verifikuan me SQL real në një databazë të veçantë: [rezultatet](docs/VERIFICATION.sq.md). Historiku i problemit të diskut: [SQL-SERVER-SETUP.sq.md](docs/SQL-SERVER-SETUP.sq.md).

Pas rregullimit të SQL, nga kjo dosje:

```powershell
dotnet restore
dotnet run --project src/FoodSales.Api -- --migrate
dotnet run --project src/FoodSales.Api -- --seed-demo
dotnet run --project src/FoodSales.Api
```

Parazgjedhja lokale është `(localdb)\MSSQLLocalDB`, databaza FoodSales. Për Express vendosni instancën që funksionon në SSMS, p.sh.:

```powershell
$env:ConnectionStrings__Shop = 'Server=.\SQLEXPRESS;Database=FoodSales;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
```

`--seed-demo` shton katër produkte ilustruese vetëm në Development dhe katalog bosh; nuk krijon shitje/klientë/pagesa. Zëvendësoni produktet dhe fotot ilustruese para përdorimit real. `Business` në appsettings mban emrin, telefonin, qytetin, adresën e marrjes dhe hartën. Lokacioni nuk është shpikur.

Për adminin e parë përdorni skriptin që kërkon fjalëkalimin privatisht, pa e shfaqur ose ruajtur në kod:

```powershell
.\scripts\Create-Admin.ps1 -Email 'emaili-juaj@example.com'
.\scripts\Start-App.ps1
```

Skriptet gjejnë edhe SDK 10 lokal. Për bootstrap të automatizuar mund të vendosni privatisht variablat `FOODSALES_ADMIN_EMAIL` dhe `FOODSALES_ADMIN_PASSWORD`, pastaj:

```powershell
dotnet run --project src/FoodSales.Api -- --create-admin
```

Nuk ka kredenciale të paracaktuara. Komanda refuzon ndryshimin e rolit të një llogarie ekzistuese. Hiqni variablat e bootstrap-it pas krijimit. Fjalëkalimi: minimum 12 karaktere, shkronja të mëdha/të vogla, numër dhe simbol.

Nëse emaili është regjistruar tashmë si klient, ndaleni aplikacionin dhe përdorni komandën e veçantë lokale për t'i dhënë rol Admin llogarisë ekzistuese:

```powershell
.\scripts\Grant-Admin.ps1 -Email 'emaili-juaj@example.com'
```

Kjo komandë përdor ASP.NET Core Identity dhe nuk ndryshon fjalëkalimin ose historikun e klientit. Pas nisjes së API-së, /api/account/me rifreskon cookie-n kur roli i ruajtur ndryshon nga roli aktual. Si Create-Admin ashtu edhe Grant-Admin lejohen vetëm nëse nuk ekziston ende asnjë administrator; kontrolli dhe bootstrap-i janë në transaksion Serializable. Pasi krijohet admini i parë, komandat CLI për adminë të tjerë refuzohen.

### Administratorë të tjerë

Hyni si admin dhe hapni **Administratorët → Shto administrator**. “Llogari e re” krijon email/fjalëkalim të ri me rolin Admin; “Llogari ekzistuese” i jep rol admin një llogarie pa i ndryshuar fjalëkalimin ose historikun. Roli i sapocaktuar rifreskohet nga /api/account/me; frontend-i provon rikuperimin e sesionit një herë pas një përgjigjeje 403.

API: GET/POST `/api/admin/administrators` dhe POST `/api/admin/administrators/grant`. Kërkohet rol Admin, token antiforgery dhe verifikim i rolit aktual në databazë. Krijimi i përdoruesit dhe rolit janë atomikë. Regjistrimi publik nuk pranon zgjedhje roli. Lista është e faqëzuar; veprimet e dhënies së rolit logohen me ID të aktorit/llogarisë dhe kohën lokale, pa fjalëkalime.

API lokale: http://localhost:5080. Në zhvillim nisni veçmas `npm run dev` në frontend; Vite përcjell `/api`, pa CORS të hapur.

## Publikimi si një app

```powershell
.\scripts\Publish-App.ps1 -FrontendPath 'C:\Users\Administrator\Desktop\food-sales-web'
```

Skripti ndërton React, e kopjon në wwwroot dhe publikon gjithë app-in në `artifacts/publish`. Për SDK-në lokale mund të jepni parametrin `-Dotnet` me rrugën e plotë. Dockerfile kërkon që React të jetë paketuar para build-it. Imazhi Docker nuk është ekzekutuar.

Para prodhimit konfiguroni SQL connection string jashtë kodit me certifikatë të verifikuar, HTTPS/reverse proxy sipas hostit, ruajtje të qëndrueshme të çelësave ASP.NET Data Protection, kopje rezervë dhe orën lokale të serverit për Kosovën. Kontejneri ka TZ=Europe/Belgrade. Mos përdorni konfigurimin LocalDB në prodhim. Asnjë shërbim cloud nuk është krijuar.

## Rregullat kryesore

- Vetëm Completed llogaritet si shitje. Pagesat ruhen veçmas dhe raportohen sipas datës së arkëtimit; shitja sipas datës së përfundimit.
- Stoku zbritet vetëm në përfundim dhe brenda transaksionit. Porositë në pritje nuk rezervojnë stok.
- Pesha reale ndryshon totalin para përfundimit; deri 3 shifra dhjetore, kurse copët janë të plota. Paratë me 2 shifra; rrumbullakim për artikull AwayFromZero.
- Shuma e papaguar në përfundim kërkon miratim admini. Pagesa e mëvonshme vetëm zvogëlon borxhin; pagesa mbi borxhin refuzohet.
- Statusi i pagesës është i ndarë nga statusi i porosisë. Anulimi nuk lejohet pas përfundimit. Rimbursimet/kthimet nuk janë implementuar.
- Çmimi i biznesit ka përparësi ndaj ofertës publike. Çmimet dhe afatet ruhen si snapshot të shitjes.
- Identity cookies HttpOnly/SameSite, antiforgery, role Admin dhe izolim sipas UserId. Telefoni nuk provon pronësi: porositë guest nuk lidhen automatikisht me llogari.
- RequestId/hash për idempotencën e porosive; RequestId unik për pagesa. Rowversion dhe transaksione Serializable për ndryshimet konkurrente. Konfliktet kërkojnë rifreskim/retry të kontrolluar.
- Lista të faqëzuara me maksimum 100; projeksione/filtra SQL dhe indekse. Pamja e çmimeve të një biznesi ka kufi 100; faqëzimi i saj i plotë mbetet për zgjerim.
- Koha përdor DateTime.Now sipas rregullave të dorëzuara; konfiguroni timezone të hostit.

## Verifikimi dhe kufijtë

Build backend: 0 gabime / 0 paralajmërime. Build TypeScript/React dhe publish i kombinuar kaluan. Migrimet u aplikuan në FoodSales; rrjedha 20 € / 12 € / 8 €, pesha/stoku dhe izolimi i llogarive kaluan përmes API-së në databazë të veçantë verifikimi. Rezultatet dhe kufijtë e mbetur janë te [VERIFICATION.sq.md](docs/VERIFICATION.sq.md). Nuk u krijuan projekte testimi sipas rregullave të përdoruesit.

Pamja vizuale në browser nuk u verifikua: hapja automatike e localhost nuk u autorizua nga porta e shfletuesit. Nuk u tentua anashkalim. Instalimi në Safari/iPhone dhe Chrome/Android duhet provuar në pajisje me HTTPS.

Fotot ngarkohen nga pajisja; URL-të ekzistuese HTTPS mbeten të mbështetura. Email/SMS/push, rikthimi i fjalëkalimit, verifikimi i emailit dhe MFA mbeten për hapat vijues. Admini konfirmon marrjen/peshën me telefon; klienti i kyçur sheh statusin në llogari. PWA ruan vetëm shell/asetet; API dhe pagesat nuk ruhen offline.

API: `/api/catalog/*`, `/api/orders`, `/api/orders/mine`, `/api/account/*`, `/api/admin/products`, `/api/admin/orders`, `/api/admin/customers`, `/api/admin/banners`, `/api/admin/dashboard`. Antiforgery: `GET /api/account/csrf`, pastaj header `X-CSRF-TOKEN`. `GET /health` kontrollon procesin, jo databazën. Raportet përdorin interval `[from,to)` me fund ekskluziv, maksimum 366 ditë.

### Fotografitë dhe rifreskimi i rolit (6 nëntor 2026)
Produktet dhe banerat pranojnë fotografi nga pajisja përmes `POST /api/admin/images` (Admin + CSRF). Formatet: JPG, PNG, WebP; deri në 5 MB. Kontrollohen prapashtesa, MIME dhe nënshkrimi i skedarit. Emri krijohet nga serveri; SQL ruan rrugën `/media/...`, ndërsa skedari ruhet jashtë wwwroot, në `ImageUploads:RootPath` (parazgjedhje `App_Data/uploads` brenda content root të API-së). Fotot shërbehen publikisht nga `/media/{fileName}`. Në hostim vendosni këtë dosje në volum persistent dhe përfshijeni në backup; disku i përkohshëm i hostit nuk siguron ruajtje pas redeploy. Heqja e fotos nga formulari heq lidhjen, jo skedarin fizik.

`GET /api/account/me` rifreskon cookie-n nëse roli aktual në databazë ndryshon nga roli i sesionit. Frontend-i rikuperon një herë sesionin kur një kërkesë admini kthen 403. Për ndryshimet e backend-it ndaloni debug-un dhe rindërtoni/rinisni API-në.

### Profili personal
`GET/PUT /api/account/profile` për klientin dhe administratorin përdorin vetëm ID-në nga sesioni. Emër, telefon, qytet, adresë ruhen te rekordi Customer i lidhur me UserId; emaili i hyrjes te Identity. Për ndryshimin e emailit kërkohet fjalëkalimi aktual (me kontroll lockout). Ruajtja e Identity dhe profilit bëhet në të njëjtin transaksion; Version mbron ndryshimet e njëkohshme. Nuk nevojitet migrim i ri. Rrugët e UI: /llogaria dhe /admin/profili. Admini ka dalje të dukshme në topbar.

Kërkesat e anuluara trajtohen si 499 vetëm kur RequestAborted është anuluar. Gabimet e tjera mbeten të raportuara. Frontend-i shtyn fillimin e leximeve një event-loop për të shmangur kërkesën e panevojshme gjatë React StrictMode effect replay. Nëse Visual Studio është konfiguruar të ndalojë në çdo hedhje exception-i, ai mund të ndalojë edhe për anulime të trajtuara; nuk është verifikuar konfigurimi lokal i debugger-it.

### Data e dëshiruar e dërgesës
Porositë online me dërgesë kërkojnë RequestedDeliveryDate (YYYY-MM-DD), jo më herët se dita aktuale sipas orës lokale të biznesit. Marrja vetë nuk kërkon datë dërgese; porositë manuale mund ta kenë opsionale. Ruhet si SQL date nullable për pajtueshmëri me porositë ekzistuese. Shfaqet në formular, konfirmim, detajet e klientit dhe listën/detajet e adminit. Data është kërkesë e klientit; biznesi konfirmon mundësinë. Migrimi AddRequestedDeliveryDate është aplikuar në LocalDB FoodSales (2026-11-06); hostet e tjera duhet të aplikojnë migrimet para nisjes së versionit të ri.
