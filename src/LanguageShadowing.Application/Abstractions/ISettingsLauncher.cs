namespace LanguageShadowing.Application.Abstractions;

public interface ISettingsLauncher
{
    Task OpenAsync(string uri);
}
