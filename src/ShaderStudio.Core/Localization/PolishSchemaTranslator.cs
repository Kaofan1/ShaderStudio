namespace ShaderStudio.Core.Localization;

/// <summary>
/// Polish labels, sections and tooltips for the shader settings. Unknown keys fall back to the
/// text from the schema file, so a shader update that adds settings still shows them correctly
/// (in English) instead of hiding them.
/// </summary>
public sealed class PolishSchemaTranslator : ISchemaTranslator
{
    public static readonly PolishSchemaTranslator Instance = new();

    private static readonly IReadOnlyDictionary<string, string> Tabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["World"] = "Świat",
        ["Vehicles & Peds"] = "Pojazdy i postacie",
        ["Lighting"] = "Oświetlenie",
        ["Image"] = "Obraz",
        ["HDR"] = "HDR",
        ["Options"] = "Opcje",
    };

    private static readonly IReadOnlyDictionary<string, string> Sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Sky"] = "Niebo",
        ["Vehicle"] = "Pojazd",
        ["Ped"] = "Postacie",
        ["Weapon"] = "Broń",
        ["Corona"] = "Korony świateł",
        ["Reflections"] = "Odbicia",
        ["Streetlights"] = "Latarnie",
        ["Streetlight Bands"] = "Pasma barw latarni",
        ["Screen Raindrops"] = "Krople na ekranie",
        ["Texture Detail"] = "Szczegóły tekstur",
        ["Motion Blur"] = "Rozmycie ruchu",
        ["Notices"] = "Komunikaty",
        ["HDR Tone Mapping"] = "Mapowanie tonów HDR",
        ["HDR Color Grading"] = "Grading kolorów HDR",
        ["General"] = "Ogólne",
    };

    private static readonly IReadOnlyDictionary<string, SettingTranslation> Settings = Build();

    public string TranslateTab(string tab) =>
        Tabs.TryGetValue(tab, out var translated) ? translated : tab;

    public string TranslateSection(string section) =>
        Sections.TryGetValue(section, out var translated) ? translated : section;

    public SettingTranslation TranslateSetting(string key, string label, string? tooltip) =>
        Settings.TryGetValue(key, out var translated)
            ? translated with { Tooltip = translated.Tooltip ?? tooltip }
            : new SettingTranslation(label, tooltip);

    /// <summary>All translated keys; exposed so the UI/tests can report coverage.</summary>
    public static IReadOnlyCollection<string> KnownSettingKeys => (IReadOnlyCollection<string>)Settings.Keys;

    private static Dictionary<string, SettingTranslation> Build() => new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Świat / niebo ---
        ["EnableVolumetricClouds"] = new("Chmury wolumetryczne", "Włącza autorskie chmury wolumetryczne."),
        ["EnableGodrays"] = new("Promienie słońca", "Wolumetryczne smugi światła przechodzące przez chmury."),
        ["EnableLightning"] = new("Błyskawice i pioruny", "Włącza błyski i pioruny podczas burzy."),
        ["CloudPerformanceMode"] = new("Wydajne chmury", "Tańsze renderowanie chmur (mniej próbek). Wyłączone = pełna jakość."),

        // --- Pojazdy i postacie ---
        ["VehicleRGBFixes"] = new("Poprawki kolorów lakieru", "Sprawia, że lakier wygląda jak kolor wybrany w palecie RGB. Gra rozjaśnia wybrane kolory (prawie czarna zieleń wychodzi jaskrawa), a ta opcja je przywraca. Wyłączone = surowa reakcja lakieru z gry. Nie dotyczy połysku, klar-lakieru, drobinek i skórki pomarańczowej."),
        ["EnableVehicleRaindrops"] = new("Krople na pojazdach", "Krople deszczu na lakierze i szybach: spływające krople ze smugami oraz statyczna mgiełka kropelek."),
        ["EnableMetallicFlakes"] = new("Drobinki metaliczne", "Iskrzenie drobinek metalicznych w lakierze pojazdu."),
        ["EnableOrangePeel"] = new("Skórka pomarańczowa", "Delikatna tekstura klar-lakieru typu \"skórka pomarańczowa\"."),
        ["EnableVehicleLights"] = new("Emisja świateł pojazdów", "Osobna regulacja jasności kloszy świateł według typu. Wyłączone = wersja vanilla."),
        ["VehLightBrightness"] = new("Jasność emisji", "Skaluje wszystkie światła pojazdu naraz. Emisja klosza to mały składnik cieniowania vanilla, więc sensowne wartości są znacznie powyżej 1."),
        ["VehLightHead"] = new("Reflektory", "Jasność kloszy reflektorów."),
        ["VehLightTail"] = new("Światła tylne", "Jasność kloszy świateł tylnych."),
        ["VehLightIndicator"] = new("Kierunkowskazy", "Jasność kloszy kierunkowskazów."),
        ["VehLightBrake"] = new("Światła hamowania", "Jasność kloszy świateł hamowania."),
        ["VehLightReverse"] = new("Światła cofania", "Jasność kloszy świateł cofania."),
        ["VehLightExtra"] = new("Dodatkowe i drogowe", "Jasność kloszy świateł dodatkowych i drogowych."),
        ["VehLightDefault"] = new("Tablice i stare sygnały", "Tablice w taksówkach/autobusach oraz klosze starszych sygnałów uprzywilejowania."),
        ["VehLightSiren"] = new("Sygnały uprzywilejowania", "Jasność nowoczesnych sygnałów uprzywilejowania."),
        ["EnablePedRaindrops"] = new("Krople na postaciach", "Krople deszczu na postaciach, zależne od faktycznego zmoczenia ciała."),
        ["EnableWeaponRaindrops"] = new("Krople na broni", "Krople deszczu na broni."),

        // --- Światło ---
        ["EnableCorona"] = new("Korony świateł", "Proceduralne korony świateł. Wyłączone = teksturowane korony z gry."),
        ["EnableStreetlights"] = new("Zmiana barwy latarni", "Zmienia kolor pomarańczowych latarni sodowych. Wyłączone = kolory z gry."),
        ["StreetlightFog_UI"] = new("Smugi światła i mgła", "Przebarwia wolumetryczne smugi światła i mgłę oświetloną przez latarnie."),
        ["StreetlightEmissiveStr"] = new("Siła świecenia lamp", "Jak mocno przebarwiane są klosze latarni."),
        ["StreetlightFogStr"] = new("Siła smug i mgły", "Jak mocno przebarwiane są smugi światła i mgła."),
        ["StreetlightLamp"] = new("Kolor lamp", "Docelowy kolor świecących kloszy latarni."),
        ["StreetlightShaft"] = new("Kolor smug i mgły", "Docelowy kolor smug światła i mgły. Obejmuje wszystkie pasma mgły."),
        ["StreetlightB1Enable"] = new("Pasmo 1", "Włącza pasmo barwne 1."),
        ["StreetlightB2Enable"] = new("Pasmo 2", "Włącza pasmo barwne 2."),
        ["StreetlightB3Enable"] = new("Pasmo 3", "Włącza pasmo barwne 3."),
        ["StreetlightB1"] = new("Kolor pasma 1", "Docelowy kolor pasma barwnego 1."),
        ["StreetlightB2"] = new("Kolor pasma 2", "Docelowy kolor pasma barwnego 2."),
        ["StreetlightB3"] = new("Kolor pasma 3", "Docelowy kolor pasma barwnego 3."),

        // --- Obraz / odbicia ---
        ["EnablePuddleSSR"] = new("Odbicia w kałużach", "Odbicia ekranowe (SSR) w kałużach na drodze. Wyłączone = odbicie kubemapy z gry."),
        ["EnableVehicleSSR"] = new("Odbicia na pojazdach", "Odbicia ekranowe (SSR) na karoserii pojazdów. Wyłączone = odbicie kubemapy z gry."),
        ["EnableWetness"] = new("Mokre nawierzchnie", "Cieniowanie mokrego podłoża: ciemniejsze i bardziej błyszczące drogi podczas deszczu."),
        ["EnableMotionBlur"] = new("Rozmycie ruchu", "Rozmycie ruchu kamery i obiektów. Wyłączone = brak rozmycia."),
        ["MotionBlurIntensity"] = new("Intensywność rozmycia", "Jak daleko sięgają smugi rozmycia."),

        // --- Krople na ekranie ---
        ["EnableScreenDrops"] = new("Krople na obiektywie", "Krople deszczu na obiektywie kamery."),
        ["ScreenDropsDefocused"] = new("Styl rozmytych kropli", "Wyłączone = realistyczne krople na szybie (domyślnie). Włączone = duże, rozmyte krążki (bokeh)."),

        // --- Szczegóły tekstur ---
        ["EnableTextureDetail"] = new("Szczegóły tekstur", "Główny przełącznik: wyłączenie przywraca shadery dróg do wersji vanilla — parallax, własne cienie, AO, dodatkowa oktawa detalu, jitter połysku i odszumianie chromy."),
        ["TexParallaxHeight"] = new("Głębokość parallaxu", "Jak głęboki wydaje się relief nawierzchni. 0 = płasko."),
        ["TexParallaxShadows"] = new("Cienie parallaxu", "Relief drogi rzuca własne, drobne cienie w stronę słońca."),
        ["TexParallaxAO"] = new("AO parallaxu", "Okluzja otoczenia w zagłębieniach reliefu drogi."),

        // --- Opcje ---
        ["HideShaderWarning"] = new("Ukryj ostrzeżenia na ekranie", "Trwale wyłącza rysowany przez dodatek komunikat \"postfx composite not loaded\". Nie zmienia sposobu renderowania — przestajesz tylko dostawać informację. Ustawienie jest pamiętane między uruchomieniami."),

        // --- HDR ---
        ["HdrToneMapType"] = new("Mapper tonów", "Mapper tonów HDR. RenoDRT rozciąga światła do szczytowej jasności (filmiczny mapper Hable→HDR używany w większości portów RenoDX) i jest zalecany, gdy HDR jest aktywny. PsychoV-17 / PsychoV-22 to mappery percepcyjne, które utrzymują średnie jasności blisko bieli papieru, a szczyt zostawiają prawdziwym światłom (słońce, refleksy, emisje)."),
        ["HdrPeakNits"] = new("Szczytowa jasność", "Szczytowa jasność monitora w nitach. Dopasuj do swojego ekranu HDR."),
        ["HdrGameNits"] = new("Jasność gry", "Biel rozproszona (biel papieru) w nitach — jasność powierzchni w 100% białej."),
        ["HdrUINits"] = new("Jasność interfejsu", "Jasność HUD i menu w nitach. Zwykle równa jasności gry."),
        ["HdrGammaCorrection"] = new("Korekcja gammy", "Kompensuje gamę 2.2, dla której powstaje większość treści SDR. BT.1886 to krzywa broadcast. Wyłączone = bez zmian w zapisie."),
        ["HdrWhiteClip"] = new("Odcięcie bieli", "Odcięcie bieli dla RenoDRT."),
        ["HdrCompression"] = new("Kompresja", "Tylko PsychoV-22: siła kompresji percepcyjnej. 1.0 = domyślna autokalibracja mappera. Mniej = mniejsza kompresja (mocniejsze światła), więcej = większa. Nie działa z innymi mapperami."),
        ["HdrConeResponse"] = new("Odpowiedź czopków", "Tylko PsychoV-17 / PsychoV-22: mnożnik wykładnika odpowiedzi czopków (wyliczanego z krzywej tonowej gry). 1.0 = domyślnie. Więcej = większy kontrast i nasycenie, mniej = płasko. Nie działa z Vanilla ani RenoDRT."),
        ["HdrGradeStrength"] = new("Siła gradacji sceny", "Siła gradacji sceny z gry (mapper tonów RenoDRT)."),
        ["HdrExposure"] = new("Ekspozycja", "Mnożnik ekspozycji. 1.0 = neutralnie."),
        ["HdrHighlights"] = new("Światła", "Poziom świateł. 50 = neutralnie."),
        ["HdrShadows"] = new("Cienie", "Poziom cieni. 50 = neutralnie."),
        ["HdrContrast"] = new("Kontrast", "Kontrast. 50 = neutralnie."),
        ["HdrSaturation"] = new("Nasycenie", "Nasycenie kolorów. 50 = neutralnie."),
        ["HdrFilmGrain"] = new("Ziarno filmowe", "Ilość ziarna filmowego. 50 = domyślnie."),
        ["HdrRandom"] = new("Losowa faza ziarna", "Wewnętrzna losowa faza animacji ziarna filmowego dla każdej klatki."),
    };
}
