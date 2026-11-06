# Verifikimi lokal — 5 nëntor 2026

## Menaxhimi i administratorëve — 6 nëntor 2026

Në databazën e veçantë FoodSales_AdminVerification_20261106_bcc823 kaluan: guest 401; klient 403 te menaxhimi; regjistrimi publik me role/isAdmin të falsifikuar mbetet klient; admini krijon një admin të ri që hyn me sukses; një klient merr rol Admin pa ndryshuar fjalëkalimin; kërkesa e përsëritur nuk dyfishon rolin; email ekzistues në krijim refuzohet me 409; fjalëkalimi i dobët refuzohet dhe nuk mbetet llogari e pjesshme; lista/kërkimi/faqëzimi funksionojnë; CLI bootstrap refuzohet pasi ekziston admini i parë. Nuk u krijuan projekte ose skedarë testimi.

Build backend dhe frontend kaluan. U shtua validim pranë fushave dhe checklist fjalëkalimi, me buton syri të aksesueshëm në hyrje/regjistrim dhe krijim admini. Build-i React u kopjua në API/wwwroot. Verifikimi vizual në browser mbetet i pakryer.

Në FoodSales u aktivizua vetëm admini i parë për emailin e zgjedhur nga përdoruesi; SQL konfirmoi FirstAdministratorActive. Fjalëkalimi ekzistues nuk u ndryshua. Për të marrë rolin në cookie duhet dalje/hyrje e re. Nuk nevojitet migrim skeme për këtë veçori.

## Korrigjimi i MapUrl — 6 nëntor 2026

Parametri opsional i konstruktorit BusinessView tani ka vlerën e parazgjedhur `MapUrl = null`, që binding-u të pranojë një vlerë null ose një çelës të munguar. Build-i i veçantë në artifacts/mapurl-check kaloi me 0 gabime dhe 0 paralajmërime. Endpoint-i GET /api/catalog/business ktheu HTTP 200 në tri raste: MapUrl null, MapUrl i munguar nga konfigurimi dhe MapUrl me URL të plotësuar. Nuk u ndryshua skema ose përmbajtja e SQL dhe nuk u krijuan skedarë/projekte testimi. Build-i normal ishte i bllokuar nga DLL-të e aplikacionit të hapur; përdoruesi duhet ta ndalë dhe ta rindërtojë në Visual Studio për ta ngarkuar korrigjimin.

## Lidhja dhe migrimet

LocalDB u verifikua me Windows Authentication nën përdoruesin e pronarit, pas rinisjes. Lidhja nga përdoruesi i sandbox-it dështon sepse LocalDB është personal; kjo nuk është defekt i aplikacionit.

Databaza kryesore `FoodSales` u krijua. Dy migrimet janë regjistruar te `__EFMigrationsHistory`, me 16 tabela për biznesin dhe Identity. Nuk u shtuan produkte, porosi, shitje ose pagesa prove në këtë databazë. Llogaria reale Admin pret që përdoruesi të zgjedhë fjalëkalimin përmes `scripts/Create-Admin.ps1`.

## Verifikim përmes API-së me SQL real

U përdor vetëm databaza `FoodSales_Verification_20261105`, me produkte dhe llogari ilustruese `example.invalid`. Fjalëkalimet e verifikimit u gjeneruan vetëm në kujtesën e procesit dhe nuk u shkruan në kod, raporte ose log-e. Databaza e verifikimit u mbajt për inspektim; serveri i saj u ndal. Nuk u shtuan projekte/librari/skedarë testimi.

Kaluan:

- Hyrja me Identity cookie dhe autorizimi Admin.
- Shitje manuale: 3 kg × 5 € + 5 l × 1 € = 20 €, pagesë 12 €, borxh 8 €.
- Raporti: shitje 20 €, arkëtime 12 €, borxh 8 €.
- Pagesë e mëvonshme 8 €: borxhi 0 €, shitje ende 20 €, arkëtime 20 €.
- Përsëritja e të njëjtës pagesë nuk krijon pagesë të dyfishtë; mbeten dy pagesa, 12 € dhe 8 €.
- Përsëritja e krijimit të shitjes me të njëjtin RequestId kthen të njëjtën porosi dhe nuk zbrit stok përsëri.
- Porosi guest e re: nuk përfshihet si shitje dhe shfaqet në pritje.
- Pesha e kërkuar 1 kg ndryshohet në 1.08 kg; totali rillogaritet në 5.40 €.
- Rrjedha online New → Confirmed → Preparing → Delivering → Completed, me pagesë të plotë 5.40 €.
- Stoku pas të dy shitjeve: djathi 80 − 3 − 1.08 = 75.92 kg; qumështi 120 − 5 = 115 l.
- Ripërdorimi i versionit të vjetër të porosisë refuzohet me 409.
- Pagesa mbi borxhin refuzohet me 422.
- Shitja me borxh pa miratim admini refuzohet me 422 dhe transaksioni kthehet prapa.
- Raporti final: shitje 25.40 €, arkëtime 25.40 €, borxh 0 €, porosi në pritje 0.
- Guest merr 401 nga API admin; klienti i kyçur merr 403; porosia e një klienti tjetër kthen 404 te endpoint-i privat.
- Klienti i ri sheh zero porosi të të tjerëve, përfshirë porositë guest.
- Kërkesa pa header antiforgery, edhe me cookie antiforgery, refuzohet me 400. Për këtë kontroll u përdor sesion i ri sepse PowerShell ruan header-at e kaluar në WebSession.
- Query i klientëve me borxhe u ekzekutua me sukses në SQL Server.
- PageSize=1000 kufizohet në maksimumin 100.

Verifikimi zbuloi dhe korrigjoi regjistrimin e filtrit antiforgery: `AddControllersWithViews` regjistron shërbimet që kërkon filtri standard. U korrigjua gjithashtu logimi që statusi HTTP të merret pas përfundimit real të përgjigjes.

## Swagger dhe komandat EF në Visual Studio

U shtuan Swashbuckle.AspNetCore 10.3.1, Microsoft.EntityFrameworkCore.Tools 10.0.14 për PMC dhe manifest lokal dotnet-ef 10.0.14. Build-i kaloi me 0 gabime / 0 paralajmërime. `dotnet tool restore`, `dotnet ef migrations list`, `dotnet ef database update` dhe `dotnet ef migrations has-pending-model-changes` kaluan: dy migrimet ekzistuese janë aplikuar dhe modeli nuk ka ndryshime të pambuluara. Nuk u krijua migrim i ri.

Në nisje të përkohshme Development, `/swagger/index.html`, `/swagger/v1/swagger.json`, initializer-i dhe bundle JavaScript kthyen HTTP 200. OpenAPI u gjenerua me version 3.0.4. JavaScript initializer kaloi `node --check`; CSP-ja e përgjithshme mbetet aktive. Kontrolli ishte përmes HTTP, pa browser; klikimet në Swagger dhe hapja automatike F5 nuk janë verifikuar në UI. Serveri i përkohshëm u ndal.

## Verifikimi i mbetur

Pamja në shfletues/pajisje, instalimi PWA në Safari/Chrome, rrjedha e marrjes vetë, ofertat me afat, çmimet e bizneseve dhe skenarët e ngarkesës së njëkohshme kërkojnë kontroll të mëtejshëm. Asnjë shërbim cloud nuk është publikuar. Kontrollet e mësipërme nuk përbëjnë mbulim të plotë testimi ose miratim për prodhim.

## 2026-11-06: Sesioni Admin dhe fotografitë
- Backend build: 0 gabime, 0 paralajmërime; frontend TypeScript/Vite/PWA build kaloi; wwwroot u përditësua.
- API smoke në LocalDB të veçuar FoodSales_SessionUpload_20261106_c917: regjistrim klienti, promovim nga admini, riprodhim 403 me cookie të vjetër, /account/me rifreskon rolin dhe i njëjti sesion merr 200 nga produktet e adminit.
- Multipart PNG real 2.2 MB: 201; emër GUID; lexim publik 200 me MIME dhe gjatësi të saktë. Skedar PNG fals dhe skedar mbi 5 MB u refuzuan me 422. Swagger JSON 200 përfshin multipart endpoint.
- Nuk u ndryshuan të dhënat e biznesit në databazën FoodSales. Nuk u krye kontroll vizual në browser në këtë verifikim.

## 2026-11-06: Profili dhe dalja e adminit
Backend build 0 gabime/paralajmërime. Smoke në FoodSales_ProfileVerification_20261106_d215: admin dhe klient ruajnë profilin; anonimi 401; profilet janë të ndara; emaili kërkon fjalëkalim; email i ri funksionon për hyrje dhe ruan rolin Admin; ndryshim me Version të vjetër 409; userId/isAdmin nga payload nuk ndryshojnë pronësinë/rolin; logout 204 dhe qasja pas logout 401. Të dhënat reale nuk u ndryshuan. Trajtimi i anulimit u shtua në pipeline; sjellja e debugger-it nuk u verifikua në Visual Studio.

## 2026-11-06: Data e dërgesës
Backend dhe frontend build kaluan. Smoke në FoodSales_DeliveryVerification_20261106_e216 kaloi: data online ruhet dhe shihet nga admin/klient; retry i njëjtë kthen të njëjtën porosi; datë e ndryshuar me të njëjtin RequestId 409; mungesë date dhe datë e kaluar 422; marrje vetë pa datë lejohet. Profilet/dalja u riverifikuan në të njëjtin kontroll. Migrimi 20261105235951_AddRequestedDeliveryDate u aplikua me sukses në FoodSales reale, duke shtuar vetëm kolonën nullable Orders.RequestedDeliveryDate. Nuk u shtuan porosi testuese në databazën reale.

## 2026-11-06: Galeritë, njoftimet, raportet dhe migrimet
- Build API me 0 gabime/paralajmërime. EF design-time factory zbulon katër migrimet reale pa ndryshore lidhjeje të vendosur në terminal; has-pending-model-changes raporton pa ndryshime të pambuluara nga migrimet. Package Manager Console nuk u kontrollua duke drejtuar UI-në e Visual Studio-s.
- Verifikim i izoluar në FoodSales_ExperienceVerification_20261106_f018: tre fotografi baneri ruhen sipas renditjes; rruga e pavlefshme refuzohet; produkti ka endpoint publik detajesh.
- Porosia e re njofton adminin dhe klientin, por jo klientin tjetër. Riprovimi i të njëjtës porosi nuk dyfishon njoftimin. Leximi ndryshon vetëm kutinë e përdoruesit; anonimi 401, klienti tjetër 404. Konfirmimi njofton klientin me datën përkatëse.
- Porosi 20 EUR në pritje: 0 shitje/borxh. Përfundim me 12 EUR: 20 shitje, 12 arkëtime, 8 borxh. Pagesa e mëvonshme 8 EUR: 20 shitje, 20 arkëtime, 0 borxh; dy pagesa dhe vetëm një porosi në histori. Filtri i borxhit dhe kufiri i faqëzimit kaluan. Klienti refuzohet me 403 nga raportet e adminit.
- Frontend TypeScript/Vite/PWA dhe Prettier kaluan. PDF u gjenerua me font Unicode të përfshirë, faqja e parë u renderua dhe u kontrollua vizualisht; XLSX u rilexua me emrin Ëndrra Çela, shuma numerike dhe dy fletë. npm audit pas korrigjimit UUID: 0 dobësi.
- Renderimi server-side konfirmoi hero vetëm te produktet dhe mungesën e tij te ofertat; karuseli ka shigjeta majtas/djathtas, ndalim dhe numërim. Kontroll vizual i aplikacionit në browser/iPhone nuk u krye.
- Migrimi i verifikuar 20261106160634_AddBannerGalleryAndPlatformNotifications u aplikua në FoodSales reale, pa të dhëna financiare testuese dhe pa fshirje të porosive/pagesave ekzistuese.

## 2026-11-06: Anulimi i njoftimeve, header-i dhe sidebar-i
- Njoftimet nuk nisin kërkesë gjatë StrictMode effect replay; hapja/riprova nuk aborton lexime në proces. Leximi shënohet lokalisht pas përgjigjes së serverit pa nisur menjëherë një lexim tjetër para navigimit.
- Controller-i trajton OperationCanceledException vetëm kur RequestAborted është anuluar; Infrastructure normalizon edhe SqlClient error 0 kur tokeni është anuluar. Gabimet pa anulim mbeten në trajtimin global.
- Prova e izoluar FoodSales_CancellationVerification_20261106_g021: një bllokim skeme brenda transaksionit të kthyer me rollback detyroi leximin të priste; klienti e anuloi; logu i kërkesës tregoi 499, pa Request failed në handler-in e API-së ose DeveloperExceptionPage. Leximi pasues ktheu 200. Nuk u kontrollua debugger-i i Visual Studio-s drejtpërdrejt.
- U hoq box-shadow i vjetër nga linku aktiv; linku dhe fokusi me tastierë përdorin vetëm nënvizim. Header-i i dyqanit ka sfond/vijë ndarëse të lehtë. Topbar-i i adminit u hoq plotësisht; njoftimet janë në fund të sidebar-it ose në navigimin mobil.
- Butoni dhe blloku reklamues shtesë u hoqën nga sidebar-i. Footer-i i sidebar-it nuk zvogëlohet; profili dhe Kthehu te dyqani mbeten në fund, ndërsa menuja ka scroll më vete. Build-i i frontend-it dhe backend-it kaluan; instalimi dhe pamja në iPhone mbeten për provë në pajisje.
