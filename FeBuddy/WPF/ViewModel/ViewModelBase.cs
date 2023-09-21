using System.ComponentModel;

namespace WPF.ViewModel;

/// <summary>
/// A class meant to serve as a base for ViewModels in the WPF application.
/// It implements the INotifyPropertyChanged interface, which is a crucial part of data binding in WPF.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    // Event that notifies when a property value changes.
    // This is used to notify the UI when a property's value changes, allowing the UI to automatically update to reflect the new value
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// A helper method used to raise the PropertyChanged event with the name of the property that has changed. 
    /// Protected method for raising the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that has changed</param>
    protected void OnPropertyChanged(string propertyName)
    {
        // Invoking the PropertyChanged event with 'this' as the sender,
        // and a new instance of PropertyChangedEventArgs containing the property name.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
