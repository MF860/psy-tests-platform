# Bilingual Font Configuration (Future Feature)

## Current Status
- **Active:** Noto Naskh Arabic (Regular + Bold) for Arabic RTL text
- **Prepared (Not Active):** Roboto font family for English LTR text

## Roboto Font Files (Placeholder)
To enable English reports in the future:

1. Download Roboto from [Google Fonts](https://fonts.google.com/specimen/Roboto)
2. Place the following files in `backend/PsyApi/Resources/Fonts/`:
   - `Roboto-Regular.ttf`
   - `Roboto-Bold.ttf`
   - `Roboto-Medium.ttf` (optional, for semi-bold)

3. Update `ReportTheme.cs` to load Roboto:
```csharp
private static SKTypeface _englishTypeface;

private static void LoadEnglishFont()
{
    var robotoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "Roboto-Regular.ttf");
    if (File.Exists(robotoPath))
    {
        using var fontStream = File.OpenRead(robotoPath);
        _englishTypeface = SKTypeface.FromStream(fontStream);
    }
}
```

4. Add language-aware text style:
```csharp
public static TextStyle TextStyle(float size, bool bold, string color, ReportLanguage lang = ReportLanguage.AR)
{
    return lang == ReportLanguage.AR 
        ? ArabicTextStyle(size, bold, color)
        : EnglishTextStyle(size, bold, color);
}
```

## Font Licensing
- **Noto Naskh Arabic:** SIL Open Font License 1.1 (free for commercial use)
- **Roboto:** Apache License 2.0 (free for commercial use)

## Notes
- English support is prepared but not activated in current version
- `LocalizationStrings.cs` contains English translations (ready for use)
- `ReportLanguage.cs` enum supports both AR/EN
- QuestPDF supports LTR text natively (no RTL helpers needed for English)
