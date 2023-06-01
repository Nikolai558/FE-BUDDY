using System.ComponentModel;

namespace WPF.BaseClasses
{
  public class ViewModelBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}