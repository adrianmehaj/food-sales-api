# Verifikimi i aplikacionit

Ky dokument ndan kontrollet teknike nga verifikimi funksional. Build-i dhe formatimi nuk provojnë vetë saktësinë e gjithë biznesit.

## Kontrolli teknik — 12 nëntor 2026

- Backend-i u ndërtua me .NET 10 pa gabime dhe pa paralajmërime.
- Formatimi C# u kontrollua me `dotnet format whitespace`; migrimet e gjeneruara përjashtohen.
- Leximi i tre produkteve ekzistuese përmes `ProductsByIdsAsync` përdori një komandë SQL. Produktet mbetën të gjurmuara për përditësimin e stokut me rowversion.
- Leximi në grup i çmimeve të veçanta përdori një komandë SQL.
- Kontrolli SQL ishte vetëm me SELECT: nuk krijoi databazë dhe nuk ndryshoi të dhëna.
- Frontend-i kaloi kontrollin TypeScript, formatimin dhe build-in production.
- Ndarja e CSS-it u krahasua me versionin para ndryshimit: i njëjti rend rregullash dhe deklaratash.
- Manifesti i build-it konfirmoi faqet dinamike në skedarë të veçantë; ato nuk përfshihen në shkarkimin fillestar të service worker-it. Asetet fillestare të referuara ekzistojnë.

Workflow-t e shtuar për GitHub kryejnë kontrollet përkatëse të build-it dhe formatimit. Ekzekutimi i tyre në GitHub duhet kontrolluar pas push-it; nuk është i barabartë me kontrollin lokal.

## Rrjedhat funksionale për regresion

Përdorni mjedis zhvillimi dhe të dhëna ilustruese. Mos ngarkoni kredenciale, eksportime klientësh ose fotografi private në repository.

| Rasti                                              | Rezultati i pritur                                 |
| -------------------------------------------------- | -------------------------------------------------- |
| Shitje 3 kg × 5 € dhe 5 l × 1 €, pagesë 12 €       | Shitje 20 €, arkëtim 12 €, borxh 8 €               |
| Pagesë e mëvonshme 8 €                             | Borxh 0 €, shitje ende 20 €, arkëtime 20 €         |
| Përsëritje me të njëjtin RequestId                 | Pa dyfishim porosie, pagese ose zbritjeje stoku    |
| Porosi e re në pritje                              | Nuk përfshihet në shitje ose borxh të përfunduar   |
| Sasi e kërkuar 1 kg, peshë reale 1.08 kg me 5 €/kg | Total 5.40 € para përfundimit                      |
| Pagesë mbi borxhin                                 | Refuzohet; nuk ruhet pagesë e pjesshme aksidentale |
| Përfundim me borxh pa miratim                      | Refuzohet dhe transaksioni kthehet prapa           |
| Dy ndryshime me të njëjtin rowversion              | I dyti refuzohet me konflikt                       |
| Guest/klient te endpoint-et e adminit              | 401/403 sipas sesionit                             |
| Klient që kërkon porosinë e dikujt tjetër          | Pa qasje në të dhënat e atij klienti               |
| Kërkesë ndryshimi pa CSRF                          | Refuzohet                                          |
| Ngarkim JPEG/JFIF, PNG, WebP i vlefshëm            | Ruajtje me emër të gjeneruar dhe formatin e njohur |
| Skedar i pavlefshëm ose tepër i madh               | Refuzohet pa publikim të skedarit                  |
| Eksport me më shumë se një faqe                    | Përfshin faqet deri në kufirin e dokumentuar       |
| Porosi dhe konfirmim                               | Njoftim për adminin dhe klientin përkatës          |

Këto rrjedha kanë pasur verifikime manuale gjatë zhvillimit; tabela është udhëzues regresioni, jo pretendim se të gjitha janë riekzekutuar pas çdo ndryshimi.

## Kufizimet

Nuk ka ende suitë të automatizuar regresioni ose testim ngarkese. Riorganizimi i fundit nuk përfshiu një provë të plotë vizuale në iPhone/Android. Instalimi, përditësimi i PWA-së dhe rrjedhat e checkout-it duhen kontrolluar në pajisje reale me HTTPS. Faqet e pavizituara mund të mos hapen offline; API-ja dhe veprimet financiare kërkojnë rrjet.

Databazat, llogaritë dhe skedarët e përkohshëm të verifikimit nuk janë pjesë e repository-t.
