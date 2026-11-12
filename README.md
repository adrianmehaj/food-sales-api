# Tryeza — Food Sales API

Backend i aplikacionit **Tryeza — Produkte Bio**, për porosi online, shitje manuale, inventar dhe menaxhim financiar. Ndërtuar me ASP.NET Core 10, Entity Framework Core 10 dhe SQL Server.

[Frontend](https://github.com/adrianmehaj/food-sales-web) · [Arkitektura](docs/ARCHITECTURE.sq.md) · [Licenca](LICENSE)

## Funksionalitetet

- Produkte me njësi kg/litër/copë, stok, fotografi dhe oferta me afat.
- Porosi online dhe shitje manuale, me dërgesë ose marrje vetë.
- Peshë përfundimtare dhe rillogaritje e totalit para përfundimit të shitjes.
- Pagesa të plota, të pjesshme ose të mëvonshme dhe historik borxhesh.
- Klientë individë/biznese, çmime të veçanta dhe afate pagese.
- Llogari personale, role administrimi dhe njoftime brenda platformës.
- Raporte financiare të faqëzuara për pasqyrën dhe klientët.

## Arkitektura

Projekti përdor Clean Architecture:

| Projekti                   | Përgjegjësia                                          |
| -------------------------- | ----------------------------------------------------- |
| `FoodSales.Api`            | Endpoint-et HTTP, autorizimi dhe middleware           |
| `FoodSales.Application`    | Rrjedhat e përdorimit, kontratat dhe ndërfaqet        |
| `FoodSales.Domain`         | Entitetet dhe rregullat e biznesit                    |
| `FoodSales.Infrastructure` | EF Core, SQL Server, Identity dhe ruajtja e skedarëve |

Domain-i është i pavarur nga databaza dhe HTTP. Ndryshimet financiare ruhen në transaksione; idempotenca dhe rowversion mbrojnë nga riprovimet dhe ndryshimet konkurrente. Për organizimin dhe varësitë shihni [dokumentimin e arkitekturës](docs/ARCHITECTURE.sq.md).

## Nisja lokale

Nevojiten .NET SDK 10 dhe SQL Server. Konfigurimi i parazgjedhur përdor SQL Server LocalDB në Windows: `(localdb)\MSSQLLocalDB`, databazën `FoodSales` dhe Windows Authentication. Për një instancë tjetër vendosni `ConnectionStrings__Shop` përmes environment variables ose user secrets.

Nga dosja e backend-it:

```powershell
dotnet restore
dotnet tool restore
dotnet ef database update --project src/FoodSales.Infrastructure --startup-project src/FoodSales.Api
dotnet run --project src/FoodSales.Api --launch-profile FoodSales.Api
```

- API: [http://localhost:5080](http://localhost:5080)
- Swagger në Development: [http://localhost:5080/swagger/index.html](http://localhost:5080/swagger/index.html)
- Kontrolli i procesit: `/health`

Në Visual Studio hapni `FoodSales.sln` dhe vendosni `FoodSales.Api` si startup project. Për migrimet në Package Manager Console zgjidhni `FoodSales.Infrastructure` si default project:

```powershell
Add-Migration "PershkrimiINdryshimit"
Update-Database
```

### Administratori fillestar

```powershell
.\scripts\Create-Admin.ps1 -Email 'admin@example.com'
```

Fjalëkalimi kërkohet privatisht. Nuk ka kredenciale të paracaktuara. Pas krijimit të administratorit të parë, administratorët e tjerë shtohen nga paneli. Regjistrimi publik krijon llogari klienti.

## Autentikimi

Përdoret ASP.NET Core Identity me cookie HttpOnly dhe kontroll qasjeje sipas rolit dhe pronësisë së të dhënave. Kërkesat që ndryshojnë të dhëna kërkojnë token nga `GET /api/account/csrf`, të dërguar në header-in `X-CSRF-TOKEN`. Dokumentimi i endpoint-eve dhe kontratave gjendet në Swagger-in lokal.

## Rregullat financiare

Shitjet regjistrohen kur porosia përfundon; arkëtimet sipas datës së pagesës. Pagesa e mëvonshme ul borxhin pa krijuar shitje të re. Borxhi i mbetur në përfundim kërkon miratimin e administratorit.

Stoku zbritet brenda transaksionit në përfundim të shitjes. Porositë në pritje nuk rezervojnë stok. Çmimet ruhen në porosi dhe çmimi i posaçëm i biznesit ka përparësi ndaj ofertës publike.

## Konfigurimi dhe publikimi

| Konfigurimi              | Qëllimi                                      |
| ------------------------ | -------------------------------------------- |
| `ConnectionStrings:Shop` | Lidhja me SQL Server                         |
| `Business`               | Të dhënat e biznesit dhe marrjes së porosive |
| `ImageUploads`           | Dosja dhe kufiri i madhësisë së fotografive  |
| `Pagination`             | Madhësia e parazgjedhur dhe kufiri i faqeve  |

Për të ndërtuar dhe publikuar frontend-in bashkë me API-në, nga dosja e backend-it:

```powershell
.\scripts\Publish-App.ps1 -FrontendPath ../food-sales-web
```

Rezultati gjenerohet në `artifacts/publish`. Frontend-i dhe API-ja shërbehen nga e njëjta adresë; SQL Server mbetet shërbim i veçantë.

Në prodhim konfiguroni HTTPS, lidhjen SQL jashtë kodit, ruajtjen e qëndrueshme të fotografive dhe çelësave Data Protection, kopjet rezervë dhe aplikimin e migrimeve. Datat financiare përdorin kohën lokale të serverit; zona kohore duhet të përputhet me biznesin.

## Kontrollet e kodit

```powershell
dotnet build FoodSales.sln --configuration Release
```

[Backend CI](.github/workflows/ci.yml) kontrollon formatimin dhe build-in në push dhe pull request, pa ndryshuar databazën. Aktualisht nuk përfshihet një suitë e automatizuar testimi regresiv.

## Licenca

[MIT](LICENSE).
