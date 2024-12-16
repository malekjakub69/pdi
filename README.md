# Distribuovaný kalkulátor prvočísel

Tento projekt je distribuovaný kalkulátor prvočísel, který využívá technologii Akka.NET a .NET 8.0.

## Instalace

Pro instalaci projektu je třeba nejprve nainstalovat nuget balíčky. To lze provést pomocí příkazu:

```bash
dotnet restore
```

## Build

Pro vytvoření buildu projektu je třeba spustit build pomocí příkazu:

```bash
dotnet build
```

## Nastavení serveru

Pro nastavení serveru pro určitý počet klientů na specifických portech je nutné upravit kód na straně serveru v souboru `Program.cs`, konkrétně řádek 39, který obsahuje pole adres klientů.

Aktuální řešení počítá s klienty na portech: 8082, 8083, 8084

Pokud navolíte nějakého klienta, který následně nebude spuštěn, server se bude snažit na něj posílat práci

## Spuštění klientů a serveru

Pro spuštění klientů a serveru je třeba spustit server a klienty. Server lze spustit pomocí příkazu:

```bash
dotnet run --project DistributedPrimeCalculator.Server
```

Klienty lze spustit pomocí příkazu:

```bash
dotnet run --project DistributedPrimeCalculator.Client -- {port}
```

## Odeslání POST dotazů

Pro odeslání POST dotazů je třeba použít nástroje pro odesílání HTTP dotazů, jako je například Postman.

Pro odeslání dotazu pro málo práce (pro 2 workery čas <1 sek) je třeba použít následující dotaz:

```
POST {{HostAddress}}/api/calculation/start?start=1&end=100000&batchSize=10000
```

Pro odeslání dotazu pro více práce (pro 2 workery čas 1 min 20 sek) je třeba použít následující dotaz:

```
POST {{HostAddress}}/api/calculation/start?start=100000000&end=200000000&batchSize=1000000
```

poz. V mém případě je {{HostAddress}} = http://localhost:5000

## Server informuje o

-   Odeslané práci na určitý worker
-   Přijaté odpovědi od workera
-   Přerozdělení práce po 30s timeoutu

## Clinet informuje o

-   Přijaté práci
-   Dokončené práci
-   Odeslané odpovědi

## Funkce a možnosti

-   Distribuovaný výpočet prvočísel v zadaném rozsahu
-   Automatické rozdělení práce mezi dostupné workery
-   Dynamické přerozdělování práce při selhání workera
-   Fronta práce
-   Podpora více workerů běžících současně
-   Real-time monitoring stavu výpočtů
-   Automatická detekce timeoutu a přerozdělení práce
-   Škálovatelnost přidáním dalších workerů

## Omezení

-   Maximální velikost zprávy je omezena na 30MB
-   Workery musí běžet na předem definovaných portech
-   Není implementováno automatické zotavení při pádu serveru
-   Výsledky jsou pouze vypisovány do konzole, chybí jejich ukládání
