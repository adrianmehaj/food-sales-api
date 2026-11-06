# Arkitektura e backend-it

```text
FoodSales.Api             → Application + Infrastructure (vetëm lidhja e shërbimeve)
FoodSales.Infrastructure  → Application → Domain
FoodSales.Domain          → pa varësi nga EF Core, SQL ose ASP.NET Core
```

- **Api:** controller-a të hollë, identiteti i përdoruesit, autorizimi, CSRF, Swagger dhe trajtimi i përbashkët i gabimeve.
- **Application:** kontratat e kërkesave/përgjigjeve, ndërfaqet dhe rrjedhat e katalogut, porosive e klientëve.
- **Domain:** entitetet dhe rregullat e porosive, peshës, përfundimit të shitjes, pagesave dhe stokut.
- **Infrastructure:** EF Core/SQL Server, Identity, repository, transaksionet, fotografitë, query-t e raporteve dhe ruajtja e njoftimeve.

Controller-at nuk përdorin ShopDbContext dhe nuk kthejnë entitete EF. Listat janë të faqëzuara me madhësi maksimale të konfiguruar. Raportet projektojnë dhe filtrojnë në SQL. Ndryshimet financiare dhe njoftimet përkatëse ruhen brenda të njëjtit transaksion. Riprovimi i një porosie nuk krijon njoftime ose shitje të dyfishta.

ILocalClock përdor kohën lokale sipas rregullave të projektit. IUnitOfWork menaxhon transaksionet; auth, profile, raporte dhe njoftime kanë ndërfaqe të veçanta. Skedarët kanë emra të gjeneruar dhe kontrolle formati, madhësie dhe autorizimi.

## Migrimet në Visual Studio

1. Hapni FoodSales.sln dhe vendosni FoodSales.Api si **Startup Project**.
2. Hapni **Tools → NuGet Package Manager → Package Manager Console**.
3. Zgjidhni **FoodSales.Infrastructure** te **Default project** (një herë për mjedisin tuaj).
4. Ndaloni debug-un nëse DLL-të janë të bllokuara dhe ekzekutoni:

```powershell
Add-Migration "EmriPershkruesINdryshimit"
Update-Database
```

Emri është emri i migrimit të skemës, jo emri i Git commit-it. Mos krijoni migrime bosh vetëm për të provuar komandën.

Nëse përzgjedhjet e Visual Studio-s janë ndryshe, format e plota janë:

```powershell
Add-Migration "EmriPershkruesINdryshimit" -Project FoodSales.Infrastructure -StartupProject FoodSales.Api -OutputDir Persistence/Migrations
Update-Database -Project FoodSales.Infrastructure -StartupProject FoodSales.Api
```

Microsoft.EntityFrameworkCore.Tools dhe Design janë në Infrastructure me versionin 10.0.14; API ka Design për projektin startup. ShopDbContextFactory lexon appsettings të API-së, konfigurimin e mjedisit, user-secrets vetëm në Development dhe environment variables. Nuk përmban fjalëkalim të shkruar në kod dhe nuk kërkon nisjen e serverit web për krijimin e kontekstit.

Add-Migration/Update-Database janë komanda të **Package Manager Console**, jo të terminalit të zakonshëm PowerShell. Në terminal të zakonshëm përdoren komandat dotnet ef të dokumentuara në README. Mos ndryshoni migrime që janë aplikuar; krijoni një migrim të ri për ndryshimet e skemës.

## GitHub Languages

GitHub tregon gjuhët e skedarëve: C# për kodin e backend-it; PowerShell për skriptet e nisjes/publikimit/bootstrap-it; HTML, JavaScript, Dockerfile e të tjera për mjetet ndihmëse. .NET dhe ASP.NET Core janë platforma/framework-e, jo emër gjuhe në atë grafik. Përqindjet nuk tregojnë cilësinë e arkitekturës. Migrimet, konfigurimet dhe skriptet e nevojshme mbahen në depo; output-et e build-it, log-et, sekretet dhe fotografitë e ngarkuara përjashtohen.
