using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;
using System.Text.Json.Nodes;
using ShaderStudio.App.Ui;
using ShaderStudio.Core.Models;
using ShaderStudio.Core.Services;

namespace ShaderStudio.App.Controls;

/// <summary>
/// Builds one settings row (label, tooltip, reset button and the right control) from a schema
/// entry. The control type is chosen from the <c>type</c> field of the setting.
/// </summary>
internal sealed class SettingRowFactory
{
    private readonly ShaderSettingsCatalog _catalog;
    private readonly Action _onChanged;
    private readonly Action _onReset;

    public SettingRowFactory(ShaderSettingsCatalog catalog, Action onChanged, Action onReset)
    {
        _catalog = catalog;
        _onChanged = onChanged;
        _onReset = onReset;
    }

    public Control Create(UiSettingDefinition definition)
    {
        var control = definition.Type switch
        {
            SettingType.Bool => CreateToggle(definition),
            SettingType.Slider => CreateSlider(definition),
            SettingType.Int => CreateChoice(definition),
            SettingType.Color => CreateColor(definition),
            _ => CreateUnsupported(definition),
        };

        var label = new TextBlock
        {
            Text = definition.Title,
            FontSize = 13,
            FontWeight = FontWeight.Medium,
            Foreground = Palette.TextPrimaryBrush,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var labelPanel = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        labelPanel.Children.Add(label);

        if (!string.IsNullOrWhiteSpace(definition.Tooltip))
        {
            labelPanel.Children.Add(new TextBlock
            {
                Text = definition.Tooltip,
                FontSize = 11,
                Foreground = Palette.TextMutedBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420,
            });
        }

        var reset = new Button
        {
            Content = "\u21BA",
            Classes = { "icon" },
            Margin = new Thickness(2, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(reset, "Przywróć wartość domyślną");
        reset.Click += (_, _) =>
        {
            // Reset shows the restored value, so the rows are rebuilt rather than patched.
            _catalog.RestoreDefault(definition.Key);
            _onReset();
        };

        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(12, 0, 0, 0);

        var inner = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
        };
        Grid.SetColumn(labelPanel, 0);
        Grid.SetColumn(control, 1);
        Grid.SetColumn(reset, 2);
        inner.Children.Add(labelPanel);
        inner.Children.Add(control);
        inner.Children.Add(reset);

        return new Border
        {
            Padding = new Thickness(16, 12),
            BorderBrush = Palette.BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = inner,
        };
    }

    private Control CreateToggle(UiSettingDefinition definition)
    {
        var toggle = new ToggleSwitch
        {
            IsChecked = ReadBool(definition.Key),
            OnContent = null,
            OffContent = null,
            VerticalAlignment = VerticalAlignment.Center,
        };

        toggle.PropertyChanged += (_, args) =>
        {
            if (args.Property == ToggleButton.IsCheckedProperty)
            {
                _catalog.TrySetValue(definition.Key, JsonValue.Create(toggle.IsChecked == true));
                _onChanged();
            }
        };

        return toggle;
    }

    private Control CreateSlider(UiSettingDefinition definition)
    {
        var value = ReadDouble(definition.Key, definition.Min);

        var slider = new Slider
        {
            Minimum = definition.Min,
            Maximum = definition.Max,
            Value = value,
            Width = 250,
            VerticalAlignment = VerticalAlignment.Center,
            TickFrequency = Math.Max((definition.Max - definition.Min) / 200.0, 0.0001),
        };

        var valueText = new TextBlock
        {
            Text = ShaderSettingsCatalog.FormatValue(definition, value),
            Width = 96,
            FontSize = 12,
            TextAlignment = TextAlignment.Right,
            Foreground = Palette.AccentBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };

        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property != RangeBase.ValueProperty)
            {
                return;
            }

            valueText.Text = ShaderSettingsCatalog.FormatValue(definition, slider.Value);
            _catalog.TrySetValue(definition.Key, JsonValue.Create(Math.Round(slider.Value, 4)));
            _onChanged();
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { slider, valueText },
        };
    }

    private Control CreateChoice(UiSettingDefinition definition)
    {
        var labels = definition.Labels.Count > 0
            ? definition.Labels
            : Enumerable.Range(0, (int)Math.Max(definition.Max, 1) + 1)
                .Select(index => index.ToString(CultureInfo.InvariantCulture))
                .ToArray();

        var selected = ReadInt(definition.Key, 0);
        if (selected < 0 || selected >= labels.Count)
        {
            selected = 0;
        }

        var combo = new ComboBox
        {
            ItemsSource = labels,
            SelectedIndex = selected,
            Width = 250,
            VerticalAlignment = VerticalAlignment.Center,
        };

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedIndex < 0)
            {
                return;
            }

            _catalog.TrySetValue(definition.Key, JsonValue.Create(combo.SelectedIndex));
            _onChanged();
        };

        return combo;
    }

    private Control CreateColor(UiSettingDefinition definition)
    {
        var channels = ReadColor(definition.Key);
        var labels = new[] { "R", "G", "B", "A" };

        var swatch = new Border
        {
            Width = 34,
            Height = 54,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Palette.BorderBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var hex = new TextBlock
        {
            FontSize = 10,
            Foreground = Palette.TextMutedBrush,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        void Refresh()
        {
            swatch.Background = new SolidColorBrush(ToColor(channels));
            hex.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"#{ToByte(channels[0]):X2}{ToByte(channels[1]):X2}{ToByte(channels[2]):X2}{ToByte(channels[3]):X2}");

            var array = new JsonArray();
            foreach (var channel in channels)
            {
                array.Add(Math.Round(channel, 4));
            }

            _catalog.TrySetValue(definition.Key, array);
            _onChanged();
        }

        for (var index = 0; index < 4; index++)
        {
            var channelIndex = index;
            var channelLabel = new TextBlock
            {
                Text = labels[index],
                FontSize = 10,
                Width = 12,
                Foreground = Palette.TextMutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = channels[index],
                Width = 84,
                VerticalAlignment = VerticalAlignment.Center,
                TickFrequency = 0.01,
            };

            var valueText = new TextBlock
            {
                Text = channels[index].ToString("0.00", CultureInfo.InvariantCulture),
                FontSize = 10,
                Width = 30,
                Foreground = Palette.TextMutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };

            slider.PropertyChanged += (_, args) =>
            {
                if (args.Property != RangeBase.ValueProperty)
                {
                    return;
                }

                channels[channelIndex] = slider.Value;
                valueText.Text = slider.Value.ToString("0.00", CultureInfo.InvariantCulture);
                Refresh();
            };

            var cell = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
            };
            cell.Children.Add(channelLabel);
            cell.Children.Add(slider);
            cell.Children.Add(valueText);

            Grid.SetColumn(cell, index % 2);
            Grid.SetRow(cell, index / 2);
            grid.Children.Add(cell);
        }

        var swatchPanel = new StackPanel
        {
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
        };
        swatchPanel.Children.Add(swatch);
        swatchPanel.Children.Add(hex);

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(swatchPanel);
        panel.Children.Add(grid);

        Refresh();
        return panel;
    }

    private static Control CreateUnsupported(UiSettingDefinition definition) => new TextBlock
    {
        Text = $"typ nieobsługiwany ({definition.Type})",
        FontSize = 11,
        Foreground = Palette.TextMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private bool ReadBool(string key)
    {
        var node = _catalog.GetValue(key);
        try
        {
            return node is not null && node.GetValue<bool>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return false;
        }
    }

    private double ReadDouble(string key, double fallback)
    {
        var node = _catalog.GetValue(key);
        try
        {
            return node?.GetValue<double>() ?? fallback;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return fallback;
        }
    }

    private int ReadInt(string key, int fallback)
    {
        var node = _catalog.GetValue(key);
        try
        {
            return node?.GetValue<int>() ?? fallback;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return fallback;
        }
    }

    private double[] ReadColor(string key)
    {
        var fallback = new[] { 1.0, 1.0, 1.0, 1.0 };
        if (_catalog.GetValue(key) is not JsonArray array)
        {
            return fallback;
        }

        var result = new double[4];
        for (var index = 0; index < 4; index++)
        {
            try
            {
                result[index] = index < array.Count
                    ? Math.Clamp(array[index]?.GetValue<double>() ?? fallback[index], 0, 1)
                    : fallback[index];
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException)
            {
                result[index] = fallback[index];
            }
        }

        return result;
    }

    private static Color ToColor(double[] channels) =>
        Color.FromArgb(ToByte(channels[3]), ToByte(channels[0]), ToByte(channels[1]), ToByte(channels[2]));

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
