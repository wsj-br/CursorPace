using CursorPace;

namespace CursorPace.ViewModels;

public sealed class LanguageChoice
{
    public LanguageChoice(LocaleInfo locale) => Locale = locale;

    public LocaleInfo Locale { get; }

    public string Code => Locale.Code;

    public override string ToString() => Locale.NativeName;

    public override bool Equals(object? obj) => obj is LanguageChoice other && other.Code == Code;

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Code);
}
