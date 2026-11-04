using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ShaderStudio.App.Controls;
using ShaderStudio.App.Ui;
using ShaderStudio.Core;
using ShaderStudio.Core.Models;
using ShaderStudio.Core.Services;
using ShaderStudio.Core.Updates;

namespace ShaderStudio.App;

/// <summary>
/// Single window of the app: installation status, schema driven settings tabs, profile
/// import/export and the automatic update flow.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly AppState _state = AppState.Load();

    private readonly StackPanel _tabList = new() { Spacing = 2 };
    private readonly StackPanel _content = new() { Spacing = 18 };

    private readonly TextBlock _installSummary = new()
    {
        FontSize = 12,
        Foreground = Palette.TextMutedBrush,
        TextWrapping = TextWrapping.Wrap,
    };

    private readonly TextBlock _statusVersion = new()
    {
        FontSize = 11,
        Foreground = Palette.TextMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly TextBlock _statusMessage = new()
    {
        FontSize = 11,
        Foreground = Palette.TextMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    private readonly TextBox _search = new()
    {
        Watermark = "Szukaj ustawień...",
        Width = 260,
        FontSize = 12,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly Button _saveButton = new()
    {
        Content = "Zapisz",
        Classes = { "primary" },
        IsEnabled = false,
    };

    private readonly Border _updateBanner;
    private readonly TextBlock _updateText = new()
    {
        FontSize = 12,
        Foreground = Palette.TextPrimaryBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
    };

    private readonly ProgressBar _updateProgress = new()
    {
        Width = 160,
        Height = 6,
        Minimum = 0,
        Maximum = 100,
        IsVisible = false,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly Button _installUpdateButton = new()
    {
        Content = "Pobierz i zainstaluj",
        Classes = { "primary" },
        IsVisible = false,
    };

    private ShaderSettingsCatalog? _catalog;
    private ShaderInstallation? _installation;
    private string? _activeTab;
    private ReleaseInfo? _availableRelease;
    private bool _busy;

    public MainWindow()
    {
        Title = $"{AppInfo.ProductName} {AppInfo.VersionDisplay}";
        Width = 1200;
        Height = 800;
        MinWidth = 940;
        MinHeight = 620;
        Background = Palette.BackgroundBrush;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _updateBanner = BuildUpdateBanner();

        Content = BuildLayout();

        Opened += (_, _) =>
        {
            LoadInitialInstallation();
            if (_state.CheckForUpdatesOnStartup)
            {
                _ = CheckForUpdatesAsync(silent: true);
            }
        };
    }

    private Control BuildLayout()
    {
        var root = new DockPanel { LastChildFill = true };

        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var status = BuildStatusBar();
        DockPanel.SetDock(status, Dock.Bottom);
        root.Children.Add(status);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("258,*") };

        var nav = BuildNavPanel();
        Grid.SetColumn(nav, 0);
        body.Children.Add(nav);

        var main = BuildMainPanel();
        Grid.SetColumn(main, 1);
        body.Children.Add(main);

        root.Children.Add(body);
        return root;
    }

    private Control BuildHeader()
    {
        var brand = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock
        {
            Text = "SHADER STUDIO",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Palette.TextPrimaryBrush,
            LetterSpacing = 1.5,
        });
        brand.Children.Add(new TextBlock
        {
            Text = "FiveM / ReShade — edytor ustawień shaderów",
            FontSize = 11,
            Foreground = Palette.TextMutedBrush,
        });

        var versionChip = new Border
        {
            Background = Palette.SurfaceAltBrush,
            BorderBrush = Palette.AccentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(10, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            Child = new TextBlock
            {
                Text = $"wersja {AppInfo.Version}",
                FontSize = 11,
                Foreground = Palette.AccentBrush,
                FontWeight = FontWeight.SemiBold,
            },
        };
        ToolTip.SetTip(versionChip, $"Wersja aplikacji: {AppInfo.Version}");

        var left = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
        };
        left.Children.Add(brand);
        left.Children.Add(versionChip);

        var pickFolder = new Button { Content = "Folder...", Classes = { "ghost" } };
        pickFolder.Click += async (_, _) => await PickFolderAsync();

        var refresh = new Button { Content = "Odśwież", Classes = { "ghost" } };
        refresh.Click += (_, _) => LoadInitialInstallation(forceDetection: true);

        var updates = new Button { Content = "Aktualizacje", Classes = { "primary" } };
        updates.Click += async (_, _) => await CheckForUpdatesAsync(silent: false);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        actions.Children.Add(pickFolder);
        actions.Children.Add(refresh);
        actions.Children.Add(updates);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(actions, 2);
        grid.Children.Add(left);
        grid.Children.Add(actions);

        return new Border
        {
            Background = Palette.SurfaceBrush,
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 14),
            Child = grid,
        };
    }

    private Control BuildNavPanel()
    {
        var panel = new StackPanel { Spacing = 14, Margin = new Thickness(16, 16, 12, 16) };

        panel.Children.Add(new TextBlock
        {
            Text = "INSTALACJA",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Palette.TextMutedBrush,
            LetterSpacing = 1.2,
        });

        var installCard = new Border
        {
            Background = Palette.SurfaceBrush,
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Child = _installSummary,
        };
        panel.Children.Add(installCard);

        panel.Children.Add(new TextBlock
        {
            Text = "ZAKŁADKI",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Palette.TextMutedBrush,
            LetterSpacing = 1.2,
            Margin = new Thickness(0, 6, 0, 0),
        });

        panel.Children.Add(_tabList);

        var linksHost = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
        _linkHost = linksHost;
        panel.Children.Add(linksHost);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0E131B")),
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new ScrollViewer
            {
                Content = panel,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            },
        };
    }

    private StackPanel _linkHost = new();

    private Control BuildMainPanel()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(20, 16, 20, 12),
            VerticalAlignment = VerticalAlignment.Center,
        };

        _search.TextChanged += (_, _) =>
        {
            RenderContent();
        };

        var revert = new Button { Content = "Cofnij zmiany", Classes = { "ghost" } };
        revert.Click += (_, _) =>
        {
            _catalog?.RestoreDefaults();
            RenderContent();
            SetMessage("Przywrócono wartości z pliku. Zapisz, aby utrwalić.", Palette.Warning);
        };

        var saveProfile = new Button { Content = "Zapisz profil", Classes = { "ghost" } };
        saveProfile.Click += async (_, _) => await SaveProfileAsync();

        var loadProfile = new Button { Content = "Wczytaj profil", Classes = { "ghost" } };
        loadProfile.Click += async (_, _) => await LoadProfileAsync();

        _saveButton.Click += (_, _) => SaveSettings();

        toolbar.Children.Add(_search);
        toolbar.Children.Add(saveProfile);
        toolbar.Children.Add(loadProfile);
        toolbar.Children.Add(revert);
        toolbar.Children.Add(_saveButton);

        var toolbarBorder = new Border
        {
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = toolbar,
        };

        var scroll = new ScrollViewer
        {
            Content = new Border { Padding = new Thickness(20, 18, 20, 28), Child = _content },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*") };
        Grid.SetRow(toolbarBorder, 0);
        Grid.SetRow(_updateBanner, 1);
        Grid.SetRow(scroll, 2);
        grid.Children.Add(toolbarBorder);
        grid.Children.Add(_updateBanner);
        grid.Children.Add(scroll);

        return grid;
    }

    private Border BuildUpdateBanner()
    {
        _installUpdateButton.Click += async (_, _) => await InstallUpdateAsync();

        var dismiss = new Button { Content = "Pomiń", Classes = { "ghost" } };
        dismiss.Click += (_, _) =>
        {
            _updateBanner.IsVisible = false;
            if (_availableRelease is not null)
            {
                _state.SkippedVersion = _availableRelease.TagName;
                _state.Save();
            }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        actions.Children.Add(_updateProgress);
        actions.Children.Add(_installUpdateButton);
        actions.Children.Add(dismiss);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var icon = new TextBlock
        {
            Text = "\u2B06",
            FontSize = 16,
            Foreground = Palette.AccentBrush,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(_updateText, 1);
        Grid.SetColumn(actions, 2);
        grid.Children.Add(icon);
        grid.Children.Add(_updateText);
        grid.Children.Add(actions);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0F2B3A")),
            BorderBrush = Palette.AccentBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 10),
            IsVisible = false,
            Child = grid,
        };
    }

    private Control BuildStatusBar()
    {
        var right = new TextBlock
        {
            FontSize = 11,
            Foreground = Palette.TextMutedBrush,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 520,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _statusPath = right;

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };
        Grid.SetColumn(_statusVersion, 0);
        Grid.SetColumn(_statusMessage, 1);
        Grid.SetColumn(right, 3);
        _statusMessage.Margin = new Thickness(14, 0, 14, 0);
        grid.Children.Add(_statusVersion);
        grid.Children.Add(_statusMessage);
        grid.Children.Add(right);

        return new Border
        {
            Background = Palette.SurfaceBrush,
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 7),
            Child = grid,
        };
    }

    private TextBlock _statusPath = new();

    private void LoadInitialInstallation(bool forceDetection = false)
    {
        ShaderInstallation? installation = null;

        if (!forceDetection && !string.IsNullOrWhiteSpace(_state.LastInstallPath))
        {
            installation = InstallLocator.TryCreate(_state.LastInstallPath);
        }

        installation ??= InstallLocator.FindAll().FirstOrDefault();

        ApplyInstallation(installation, notify: forceDetection);
    }

    private void ApplyInstallation(ShaderInstallation? installation, bool notify)
    {
        _installation = installation;
        _catalog = null;

        if (installation is not null)
        {
            try
            {
                _catalog = ShaderSettingsCatalog.Load(installation.SettingsPath, ShaderStudio.Core.Localization.PolishSchemaTranslator.Instance);
            }
            catch (Exception ex)
            {
                SetMessage($"Nie można wczytać settings.json: {ex.Message}", Palette.Danger);
            }

            _state.LastInstallPath = installation.RootDirectory;
            _state.Save();
        }

        _activeTab = _catalog?.Tabs.FirstOrDefault();
        RebuildNav();
        RenderContent();
        UpdateStatus();

        if (notify)
        {
            SetMessage(
                installation is null ? "Nie znaleziono instalacji shaderów." : "Wczytano ustawienia z pliku.",
                installation is null ? Palette.Warning : Palette.Success);
        }
    }

    private void RebuildNav()
    {
        _tabList.Children.Clear();

        if (_installation is null)
        {
            _installSummary.Text = "Nie znaleziono folderu z shaderami.\nKliknij \"Folder...\" i wskaż folder plugins FiveM.";
        }
        else
        {
            _installSummary.Text = $"{_installation.Kind}\n{_installation.ShaderFileCount} shaderów | ReShade: {(_installation.HasReShade ? "tak" : "brak")}\nAddon: {_installation.AddonName ?? "brak"}\n\n{_installation.RootDirectory}";
        }

        _linkHost.Children.Clear();
        if (_catalog is { Links.Count: > 0 })
        {
            _linkHost.Children.Add(new TextBlock
            {
                Text = "LINKI",
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = Palette.TextMutedBrush,
                LetterSpacing = 1.2,
            });

            foreach (var link in _catalog.Links)
            {
                var button = new Button { Content = link.Label, Classes = { "nav" } };
                if (!string.IsNullOrWhiteSpace(link.Tooltip))
                {
                    ToolTip.SetTip(button, link.Tooltip);
                }

                var url = link.Url;
                button.Click += async (_, _) => await OpenUrlAsync(url);
                _linkHost.Children.Add(button);
            }
        }

        if (_catalog is null)
        {
            return;
        }

        foreach (var tab in _catalog.Tabs)
        {
            var button = new Button
            {
                Content = tab,
                Classes = { "nav" },
                Tag = tab,
            };

            if (string.Equals(tab, _activeTab, StringComparison.OrdinalIgnoreCase))
            {
                button.Classes.Add("active");
            }

            button.Click += (_, _) =>
            {
                _activeTab = tab;
                _search.Text = string.Empty;
                RebuildNav();
                RenderContent();
            };

            _tabList.Children.Add(button);
        }
    }

    private void RenderContent()
    {
        _content.Children.Clear();

        if (_catalog is null)
        {
            _content.Children.Add(BuildNoInstallPanel());
            return;
        }

        var factory = new SettingRowFactory(_catalog, OnValueChanged, RenderContent);
        var search = _search.Text?.Trim() ?? string.Empty;

        IEnumerable<UiSettingDefinition> query = _catalog.Settings.Where(setting => !setting.Hidden);
        if (search.Length > 0)
        {
            query = query.Where(setting => Matches(setting, search));
        }
        else if (_activeTab is not null)
        {
            query = query.Where(setting => string.Equals(setting.Tab, _activeTab, StringComparison.OrdinalIgnoreCase));
        }

        var sections = query.GroupBy(setting => setting.Section).ToList();
        if (sections.Count == 0)
        {
            _content.Children.Add(new TextBlock
            {
                Text = search.Length > 0 ? "Brak ustawień pasujących do wyszukiwania." : "Ta zakładka nie ma ustawień.",
                FontSize = 12,
                Foreground = Palette.TextMutedBrush,
            });
            return;
        }

        foreach (var section in sections)
        {
            _content.Children.Add(new TextBlock
            {
                Text = section.Key,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = Palette.AccentBrush,
                LetterSpacing = 0.8,
            });

            var rows = new StackPanel();
            foreach (var setting in section)
            {
                rows.Children.Add(factory.Create(setting));
            }

            _content.Children.Add(new Border
            {
                Background = Palette.SurfaceBrush,
                BorderBrush = Palette.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Child = rows,
            });
        }

        if (search.Length > 0)
        {
            _content.Children.Add(new TextBlock
            {
                Text = $"Znaleziono {sections.Sum(section => section.Count())} ustawień.",
                FontSize = 11,
                Foreground = Palette.TextMutedBrush,
            });
        }
    }

    private Control BuildNoInstallPanel()
    {
        var panel = new StackPanel { Spacing = 12 };

        panel.Children.Add(new TextBlock
        {
            Text = "Brak wczytanej instalacji",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Palette.TextPrimaryBrush,
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Shader Studio edytuje plik CustomShaders\\settings.json z Twojej własnej instalacji shaderów.\n"
                 + "Aplikacja nie zawiera żadnych shaderów ani plików ReShade — pracuje tylko na plikach, które już masz.\n\n"
                 + "1. Kliknij \"Folder...\" u góry.\n"
                 + "2. Wskaż folder plugins FiveM (ten z podfolderem CustomShaders),\n"
                 + "   np. %LOCALAPPDATA%\\FiveM\\FiveM.app\\plugins\n"
                 + "3. Ustawienia pojawią się jako zakładki po lewej.",
            FontSize = 12,
            Foreground = Palette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 720,
            LineHeight = 20,
        });

        var pick = new Button { Content = "Wybierz folder...", Classes = { "primary" } };
        pick.Click += async (_, _) => await PickFolderAsync();
        panel.Children.Add(pick);

        return new Border
        {
            Background = Palette.SurfaceBrush,
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(22),
            Child = panel,
        };
    }

    private static bool Matches(UiSettingDefinition setting, string search) =>
        setting.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
        || setting.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
        || setting.Section.Contains(search, StringComparison.OrdinalIgnoreCase)
        || (setting.Tooltip?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

    private void OnValueChanged() => UpdateStatus();

    private void UpdateStatus()
    {
        _statusVersion.Text = $"Wersja aplikacji: {AppInfo.Version}";
        _statusPath.Text = _catalog?.FilePath ?? _installation?.RootDirectory ?? "brak instalacji";
        _saveButton.IsEnabled = _catalog?.IsDirty == true;

        if (_catalog is null)
        {
            return;
        }

        _statusMessage.Text = _catalog.IsDirty
            ? "Zmiany niezapisane — plik settings.json nie został jeszcze zmieniony."
            : "Brak zmian.";
        _statusMessage.Foreground = _catalog.IsDirty ? Palette.WarningBrush : Palette.TextMutedBrush;
    }

    private void SaveSettings()
    {
        if (_catalog is null)
        {
            return;
        }

        try
        {
            var backup = _catalog.Save();
            SetMessage(
                backup is null
                    ? "Zapisano ustawienia."
                    : $"Zapisano ustawienia. Kopia poprzedniej wersji: {Path.GetFileName(backup)}",
                Palette.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetMessage($"Nie udało się zapisać: {ex.Message}", Palette.Danger);
        }

        UpdateStatus();
    }

    private void SetMessage(string message, Color color)
    {
        _statusMessage.Text = message;
        _statusMessage.Foreground = new SolidColorBrush(color);
    }

    private async Task PickFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Wybierz folder plugins FiveM (z podfolderem CustomShaders)",
            AllowMultiple = false,
        });

        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].Path.LocalPath;
        var installation = InstallLocator.TryCreate(path);
        if (installation is null)
        {
            SetMessage("Ten folder nie zawiera CustomShaders\\settings.json.", Palette.Danger);
            return;
        }

        ApplyInstallation(installation, notify: false);
        SetMessage("Wczytano ustawienia z wybranego folderu.", Palette.Success);
    }

    private async Task SaveProfileAsync()
    {
        if (_catalog is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz profil ustawień",
            SuggestedFileName = "profil-shaderow.json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { JsonFileType() },
        });

        if (file is null)
        {
            return;
        }

        var profile = ProfileSerializer.Create(_catalog, Path.GetFileNameWithoutExtension(file.Name));
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(ProfileSerializer.Serialize(profile));

        SetMessage($"Zapisano profil \"{profile.Name}\".", Palette.Success);
    }

    private async Task LoadProfileAsync()
    {
        if (_catalog is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Wczytaj profil ustawień",
            AllowMultiple = false,
            FileTypeFilter = new[] { JsonFileType() },
        });

        if (files.Count == 0)
        {
            return;
        }

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            var profile = ProfileSerializer.Deserialize(await reader.ReadToEndAsync());
            var applied = _catalog.ApplyValues(profile.Values);

            RenderContent();
            UpdateStatus();
            SetMessage($"Wczytano profil \"{profile.Name}\" ({applied} ustawień). Zapisz, aby utrwalić.", Palette.Success);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            SetMessage($"Nie udało się wczytać profilu: {ex.Message}", Palette.Danger);
        }
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (_busy)
        {
            return;
        }

        if (!AppInfo.IsUpdateChannelConfigured)
        {
            if (!silent)
            {
                SetMessage(
                    "Kanał aktualizacji nie jest skonfigurowany (AppInfo.RepositoryOwner). Zobacz README.",
                    Palette.Warning);
            }

            return;
        }

        _busy = true;
        if (!silent)
        {
            SetMessage("Sprawdzanie aktualizacji...", Palette.TextMuted);
        }

        try
        {
            using var client = new GitHubUpdateClient();
            var result = await client.CheckAsync();

            if (result.IsUpdateAvailable && result.Latest is not null)
            {
                _availableRelease = result.Latest;

                if (!string.Equals(_state.SkippedVersion, result.Latest.TagName, StringComparison.OrdinalIgnoreCase))
                {
                    _updateText.Text = $"Dostępna aktualizacja {result.Latest.TagName} (uzywasz {AppInfo.VersionDisplay}).";
                    _updateBanner.IsVisible = true;
                }

                SetMessage($"Nowa wersja: {result.Latest.TagName}.", Palette.Accent);
            }
            else
            {
                _updateBanner.IsVisible = false;
                SetMessage($"Masz najnowszą wersję ({AppInfo.VersionDisplay}).", Palette.Success);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            SetMessage($"Nie można sprawdzić aktualizacji: {ex.Message}", Palette.Warning);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task InstallUpdateAsync()
    {
        var release = _availableRelease;
        var asset = release?.Installer;
        if (release is null || asset is null)
        {
            SetMessage("Ta wersja nie ma załącznika do pobrania.", Palette.Warning);
            return;
        }

        _installUpdateButton.IsEnabled = false;
        _updateProgress.IsVisible = true;
        _updateProgress.Value = 0;
        SetMessage($"Pobieranie {asset.Name}...", Palette.TextMuted);

        try
        {
            var staged = UpdateInstaller.GetStagedExecutablePath(release.Version);
            var progress = new Progress<double>(value => _updateProgress.Value = value * 100);

            using var client = new GitHubUpdateClient();
            await client.DownloadAsync(asset, staged, progress);

            var script = UpdateInstaller.WriteApplyScript(staged, UpdateInstaller.CurrentExecutablePath, Environment.ProcessId);
            UpdateInstaller.LaunchApplyScript(script);

            SetMessage("Aktualizacja gotowa — aplikacja zostanie uruchomiona ponownie.", Palette.Success);
            await Task.Delay(400);
            Close();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            _installUpdateButton.IsEnabled = true;
            _updateProgress.IsVisible = false;
            SetMessage($"Aktualizacja nie powiodła się: {ex.Message}", Palette.Danger);
        }
    }

    private async Task OpenUrlAsync(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            SetMessage($"Nie można otworzyć linku: {url}", Palette.Warning);
        }
    }

    private static FilePickerFileType JsonFileType() => new("Profil Shader Studio")
    {
        Patterns = new[] { "*.json" },
        MimeTypes = new[] { "application/json" },
    };
}
