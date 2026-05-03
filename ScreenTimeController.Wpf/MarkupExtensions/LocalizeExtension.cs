using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ScreenTimeController.Wpf.MarkupExtensions;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var value = LocalizeData.GetOrCreate(Key);
        
        var binding = new Binding(nameof(LocalizeData.Value))
        {
            Source = value,
            Mode = BindingMode.OneWay
        };
        
        return binding.ProvideValue(serviceProvider);
    }
}

public class LocalizeData : DependencyObject
{
    private static readonly Dictionary<string, LocalizeData> Cache = new();

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(LocalizeData),
            new PropertyMetadata(string.Empty));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Key { get; }

    private LocalizeData(string key)
    {
        Key = key;
        UpdateValue();
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateValue();
    }

    private void UpdateValue()
    {
        try
        {
            Value = LanguageManager.GetString(Key);
        }
        catch
        {
            Value = Key;
        }
    }

    public static LocalizeData GetOrCreate(string key)
    {
        if (!Cache.TryGetValue(key, out var data))
        {
            data = new LocalizeData(key);
            Cache[key] = data;
        }
        return data;
    }
}
