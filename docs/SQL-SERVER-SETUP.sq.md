# Lidhja lokale me SQL Server

## Diagnostikimi i verifikuar më 5 nëntor 2026

- Kompjuteri quhet `ADRIANS-LENOVO`.
- SSMS provonte adresën `DESKTOP-IVHRL9V\SQLEXPRESS01` dhe kthente gabimin 26.
- Nuk u gjet shërbim i regjistruar `MSSQLSERVER`, `MSSQL$SQLEXPRESS` ose `MSSQL$SQLEXPRESS01`. `SQLBrowser` ekziston por është i ndalur.
- Ekzistojnë komponentët e instalimit SQL Server 2022 dhe dosjet e dy instalimeve të dështuara; kjo nuk provon që një instancë funksionale ekziston.
- `Summary.txt` i instalimit raporton `Wait on the Database Engine recovery handle failed`.
- `ERRORLOG` për SQLEXPRESS01 dhe LocalDB raporton `256 misaligned log IOs`.
- `fsutil fsinfo sectorinfo C:` raporton `PhysicalBytesPerSectorForPerformance: 16384`, pra 16 KB.

Këto prova përputhen me problemin e sektorëve më të mëdhenj se 4 KB të dokumentuar nga Microsoft. Emri i vjetër i serverit është një problem shtesë. Aktivizimi i SQLBrowser ose ndryshimi i fjalëkalimit nuk riparon instalimin e dështuar.

Burimi: https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/database-file-operations/troubleshoot-os-4kb-disk-sector-size

## 1. Rregullimi i përputhshmërisë së diskut

Këto hapa ndryshojnë Windows Registry dhe kërkojnë rinisje. Nuk janë ekzekutuar nga agjenti. Hapni **Command Prompt → Run as administrator**.

Ruani më parë një kopje rezervë të konfigurimit ekzistues:

```bat
reg export "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters" "%USERPROFILE%\Desktop\stornvme-before-sql.reg" /y
```

Nëse eksportimi dështon, ndaloni dhe kontrolloni gabimin para ndryshimit. Pastaj përdorni komandën e dokumentuar nga Microsoft:

```bat
REG ADD "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device" /v "ForcedPhysicalSectorSizeInBytes" /t REG_MULTI_SZ /d "* 4095" /f
REG QUERY "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device" /v "ForcedPhysicalSectorSizeInBytes"
```

Ruani punën dhe rinisni kompjuterin vetë. Pas rinisjes kontrolloni përsëri:

```bat
fsutil fsinfo sectorinfo C:
```

Vlera efektive e sektorit e përdorur për SQL Server duhet të jetë e mbështetur (512 ose 4096 bytes). Ndiqni artikullin e Microsoft për interpretimin e vlerave. Nëse problemi vazhdon, mos krijoni vazhdimisht instanca të reja.

## 2. Verifikoni një instancë funksionale

Për zhvillim mund të provohet LocalDB ekzistues pas rinisjes:

```bat
"C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe" start MSSQLLocalDB
```

Nëse niset, SSMS lidhet me `(localdb)\MSSQLLocalDB`, Windows Authentication. Backend përdor këtë adresë në konfigurimin fillestar.

Për instancë SQL Server Express të plotë, hapni instaluesin zyrtar SQL Server Express dhe kontrolloni instalimet e dështuara. Mos fshini baza ekzistuese. Nëse nuk ka të dhëna për t'u ruajtur, hiqni vetëm komponentin e instancës së dështuar dhe riinstaloni Database Engine Services sipas udhëzimit të Microsoft. Mund të përdorni emrin `SQLEXPRESS`. Zgjidhni Windows Authentication dhe shtoni përdoruesin aktual si administrator SQL. SSMS është mjeti i menaxhimit; Database Engine është serveri.

Shkarkimi zyrtar: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

Në `services.msc` duhet të shfaqet **SQL Server (SQLEXPRESS)** me status **Running**. Nëse instanca ka emër tjetër, përdorni atë emër saktësisht.

## 3. Lidhja në SSMS

Për instancën `SQLEXPRESS` në të njëjtin kompjuter:

- Server type: Database Engine
- Server name: `.\SQLEXPRESS` (ose `ADRIANS-LENOVO\SQLEXPRESS`)
- Authentication: Windows Authentication
- Trust server certificate: aktivizojeni vetëm për zhvillimin lokal nëse përdoret certifikatë e vetënënshkruar.

Nëse instanca e verifikuar quhet `SQLEXPRESS01`, përdorni `.\SQLEXPRESS01`. Mos përdorni `DESKTOP-IVHRL9V` nëse serveri nuk është realisht në atë kompjuter tjetër.

## 4. Lidhja e backendit

Nga dosja e backendit, në PowerShell (ndërroni instancën sipas asaj që funksionon në SSMS):

```powershell
$env:ConnectionStrings__Shop = 'Server=.\SQLEXPRESS;Database=FoodSales;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FoodSales.Api -- --migrate
dotnet run --project src/FoodSales.Api -- --seed-demo
dotnet run --project src/FoodSales.Api
```

Migrimi krijon skemën FoodSales; nuk krijon ose instalon serverin SQL. Seed-i është vetëm për zhvillim dhe shton produkte ilustruese, jo shitje ose klientë realë. Porti i aplikacionit: http://localhost:5080.

Në prodhim përdorni connection string me TLS të verifikuar (`TrustServerCertificate=False`) dhe sekrete jashtë kodit. Mos publikoni SQL Server drejtpërdrejt në internet.
