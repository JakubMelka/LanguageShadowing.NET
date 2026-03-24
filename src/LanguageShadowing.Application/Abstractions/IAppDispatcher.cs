namespace LanguageShadowing.Application.Abstractions;

public interface IAppDispatcher
{
    Task RunAsync(Action action);
}
