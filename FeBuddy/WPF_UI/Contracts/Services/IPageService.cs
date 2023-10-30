using System.Windows.Controls;

namespace WPF_UI.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    Page GetPage(string key);
}
