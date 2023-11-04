using System.Windows.Controls;

namespace WPF_Template.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    Page GetPage(string key);
}
