# Shader Studio for FiveM

Nieoficjalna aplikacja desktopowa (.NET 10 + Avalonia) do wygodnej edycji ustawień shaderów ReShade
używanych na serwerach **FiveM**. Zamiast klikać po menu ReShade w grze, dostajesz normalne okno
z zakładkami, wyszukiwarką, presetami i automatycznymi aktualizacjami — **w całości po polsku**.

> **To nie jest oficjalne narzędzie CoreFX ani ReShade.** Projekt nie jest powiązany z autorami
> shaderów, Rockstar Games ani Take-Two. Nazwy produktów należą do ich właścicieli.

Repozytorium: <https://github.com/Kaofan1/ShaderStudio>

---

## Instalacja (Windows)

**Instalator MSI** – pobierz `ShaderStudio-Setup-X.Y.Z.msi` z zakładki *Releases* i uruchom go dwuklikiem.

- instaluje dla bieżącego użytkownika, **bez pytania o uprawnienia administratora**,
- pliki trafiają do `%LOCALAPPDATA%\Programs\Shader Studio`,
- dodaje skrót „Shader Studio” w menu Start oraz wpis w *Dodaj lub usuń programy*,
- odinstalowanie: ten sam wpis w *Dodaj lub usuń programy* albo skrót „Odinstaluj Shader Studio”,
- instalacja w folderze użytkownika jest celowa: dzięki temu auto-aktualizacja może podmienić plik
  `.exe` bez uprawnień administratora.

**Wersja przenośna** – `ShaderStudio.exe` z *Releases* to jeden samodzielny plik (self-contained),
który wystarczy położyć w dowolnym folderze.

## Co robi aplikacja

- **Wykrywa instalację** – szuka folderu `plugins` FiveM (`%LOCALAPPDATA%\FiveM\FiveM.app\plugins`)
  i sprawdza, czy zawiera `CustomShaders\settings.json`. Folder można też wskazać ręcznie.
- **Czyta schemat ustawień** z `settings.json` (`tabs`, `groups`, `ui_settings`, `links`) i buduje
  z niego interfejs: przełączniki, suwaki, listy wyboru i kolory RGBA.
- **Polski interfejs** – nazwy zakładek, sekcji, ustawień i podpowiedzi są tłumaczone w
  `src/ShaderStudio.Core/Localization/PolishSchemaTranslator.cs`. Klucze ustawień pozostają
  oryginalne, więc plik i profile są w pełni zgodne z dodatkiem. Nowe ustawienia, których nie ma
  w słowniku, pokazują się po angielsku (nic nie znika).
- **Zapisuje zmiany do tego samego pliku**, zachowując pola, których aplikacja nie zna. Przed
  zapisem powstaje kopia `settings.json.bak`, a zapis jest atomowy (plik tymczasowy + podmiana).
- **Wyszukiwarka** ustawień po nazwie, kluczu, sekcji i podpowiedzi.
- **Przywracanie domyślnych** – globalnie („Cofnij zmiany”) lub pojedynczo (ikona ↺ przy wierszu).
- **Profile** – eksport/import aktualnych wartości do pliku JSON, np. `deszcz.json`, `sucho.json`.
- **Wersja aplikacji widoczna w środku**: plakietka `wersja X.Y.Z` w nagłówku, pełny tekst
  `Wersja aplikacji: X.Y.Z` w pasku stanie oraz w tytule okna.
- **Automatyczne aktualizacje** z GitHub Releases.

## Wymagania

- Windows 10/11 x64.
- **.NET SDK 10** do zbudowania ze źródeł ([pobierz](https://dotnet.microsoft.com/download)).
  Do uruchomienia gotowej aplikacji SDK nie jest potrzebny.
- Własna, legalnie zdobyta instalacja shaderów (np. CoreFX) w folderze FiveM. Aplikacja **nie
  zawiera** żadnych shaderów ani plików ReShade.

## Budowanie

```bash
dotnet restore ShaderStudio.slnx
dotnet build  ShaderStudio.slnx -c Release
dotnet test   ShaderStudio.slnx -c Release

# wersja przenośna, jeden plik exe:
dotnet publish src/ShaderStudio.App/ShaderStudio.App.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish

# instalator MSI (wymaga WiX 5 jako narzędzia .NET):
dotnet tool install --global wix --version 5.*
wix build installer/ShaderStudio.wxs -d ProductVersion=1.0.0.0 \
  -o installer/out/ShaderStudio-Setup-1.0.0.msi -arch x64
```

`publish/ShaderStudio.exe` i `installer/out/*.msi` to gotowe artefakty.

### Tryb diagnostyczny (bez okna)

```bash
ShaderStudio.exe --diagnose
ShaderStudio.exe --diagnose --path "%LOCALAPPDATA%\FiveM\FiveM.app\plugins"
ShaderStudio.exe --diagnose --check-updates
```

Kod wyjścia: `0` = OK, `2` = brak instalacji, `3` = błąd schematu, `4` = błąd sprawdzania aktualizacji.

## Automatyczne aktualizacje

Aplikacja korzysta z **GitHub Releases** – bez własnego serwera.

1. Numer wersji jest w jednym miejscu: `Directory.Build.props` (`<Version>`), a workflow nadpisuje go
   tagiem (`-p:Version=...`). Dzięki temu wersja w aplikacji zawsze odpowiada wydaniu.
2. Wypuszczanie wersji:

   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```

3. Workflow `.github/workflows/release.yml` buduje `ShaderStudio.exe`, MSI, sumy SHA-256 i tworzy
   Release z tymi plikami.

Przy starcie aplikacja sprawdza `releases/latest`; przy nowszej wersji pokazuje pasek „Dostępna
aktualizacja”. Po kliknięciu pobiera plik, **weryfikuje SHA-256** (z pola `digest` wydania na
GitHubie), a następnie uruchamia skrypt PowerShell, który czeka na zamknięcie aplikacji, podmienia
`.exe` i startuje ją ponownie. Wersję można pominąć przyciskiem „Pomiń”.

Repozytorium aktualizacji ustawia się w `src/ShaderStudio.Core/AppInfo.cs`
(`DefaultRepositoryOwner`, `DefaultRepositoryName`) lub zmiennymi `SHADERSTUDIO_REPO_OWNER`
i `SHADERSTUDIO_REPO_NAME`. Token `SHADERSTUDIO_GITHUB_TOKEN` (opcjonalny) podnosi limit API GitHuba.

## Struktura

```
src/ShaderStudio.Core/          schemat, wykrywanie instalacji, tłumaczenia, profile, aktualizacje
src/ShaderStudio.App/           UI (Avalonia, budowane w kodzie – sterowane schematem)
src/ShaderStudio.App/Assets/    ikona aplikacji
installer/ShaderStudio.wxs      definicja instalatora MSI (WiX 5)
tests/ShaderStudio.Core.Tests/  testy xunit
.github/workflows/              CI (build + testy) i wydanie (tag v*)
```

## Uwaga prawna

- W repozytorium **nie ma i nie może być** plików shaderów (np. CoreFX), `dxgi.dll` ani presetów
  ReShade – ich licencje zabraniają redystrybucji. Folder `CoreFX_Legacy_FiveM/` jest wpisany do
  `.gitignore` i służy wyłącznie jako lokalny materiał testowy.
- Aplikacja modyfikuje tylko plik konfiguracyjny już zainstalowanych shaderów, na komputerze
  użytkownika, i nie dołącza do niego żadnych treści.
- Licencja kodu aplikacji: MIT (plik `LICENSE`).

*Projekt nieoficjalny. Grand Theft Auto V, FiveM, ReShade i CoreFX należą do ich właścicieli.*
