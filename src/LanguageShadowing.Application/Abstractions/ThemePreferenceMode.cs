namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Specifies how the application theme should be resolved.
/// </summary>
public enum ThemePreferenceMode
{
    /// <summary>Follow the operating-system theme.</summary>
    System,

    /// <summary>Force the light theme.</summary>
    Light,

    /// <summary>Force the dark theme.</summary>
    Dark
}
