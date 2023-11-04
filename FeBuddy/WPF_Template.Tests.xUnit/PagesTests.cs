using System.IO;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WPF_Template.Contracts.Services;
using WPF_Template.Core.Contracts.Services;
using WPF_Template.Core.Services;
using WPF_Template.Models;
using WPF_Template.Services;
using WPF_Template.ViewModels;
using WPF_Template.Views;

using Xunit;

namespace WPF_Template.Tests.XUnit;

public class PagesTests
{
    private readonly IHost _host;

    public PagesTests()
    {
        var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.SetBasePath(appLocation))
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IFileService, FileService>();

        // Services
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<ISystemService, SystemService>();
        services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
        services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<Sct2FileConversionViewModel>();
        services.AddTransient<RWRadarVideoMapConversionViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<InformationViewModel>();
        services.AddTransient<HelpViewModel>();
        services.AddTransient<AiracUpdateViewModel>();

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
    }

    // TODO: Add tests for functionality you add to SettingsViewModel.
    [Fact]
    public void TestSettingsViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(SettingsViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetSettingsPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(SettingsViewModel).FullName);
            Assert.Equal(typeof(SettingsPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to Sct2FileConversionViewModel.
    [Fact]
    public void TestSct2FileConversionViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(Sct2FileConversionViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetSct2FileConversionPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(Sct2FileConversionViewModel).FullName);
            Assert.Equal(typeof(Sct2FileConversionPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to RWRadarVideoMapConversionViewModel.
    [Fact]
    public void TestRWRadarVideoMapConversionViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(RWRadarVideoMapConversionViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetRWRadarVideoMapConversionPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(RWRadarVideoMapConversionViewModel).FullName);
            Assert.Equal(typeof(RWRadarVideoMapConversionPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to MainViewModel.
    [Fact]
    public void TestMainViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(MainViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetMainPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(MainViewModel).FullName);
            Assert.Equal(typeof(MainPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to InformationViewModel.
    [Fact]
    public void TestInformationViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(InformationViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetInformationPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(InformationViewModel).FullName);
            Assert.Equal(typeof(InformationPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to HelpViewModel.
    [Fact]
    public void TestHelpViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(HelpViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetHelpPageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(HelpViewModel).FullName);
            Assert.Equal(typeof(HelpPage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }

    // TODO: Add tests for functionality you add to AiracUpdateViewModel.
    [Fact]
    public void TestAiracUpdateViewModelCreation()
    {
        var vm = _host.Services.GetService(typeof(AiracUpdateViewModel));
        Assert.NotNull(vm);
    }

    [Fact]
    public void TestGetAiracUpdatePageType()
    {
        if (_host.Services.GetService(typeof(IPageService)) is IPageService pageService)
        {
            var pageType = pageService.GetPageType(typeof(AiracUpdateViewModel).FullName);
            Assert.Equal(typeof(AiracUpdatePage), pageType);
        }
        else
        {
            Assert.True(false, $"Can't resolve {nameof(IPageService)}");
        }
    }
}
