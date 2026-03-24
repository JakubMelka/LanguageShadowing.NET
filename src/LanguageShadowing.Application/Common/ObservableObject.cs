using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LanguageShadowing.Application.Common;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void OnPropertyChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    protected static ReadOnlyObservableCollection<T> AsReadOnly<T>(ObservableCollection<T> collection)
    {
        return new ReadOnlyObservableCollection<T>(collection);
    }
}
