using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows.Threading;
using System.IO;
using System.Reflection;
using System.Windows;
using FeBuddyWPF.Contracts.Services;
using FeBuddyWPF.Models;
using FeBuddyWPF.Views;
using FeBuddyWPF.ViewModels;
using FeBuddyWPF.Services;
using FeBuddyWPF.Contracts.Views;

namespace FeBuddyWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;

        public T GetService<T>() where T : class => _host.Services.GetService(typeof(T)) as T;

        public App()
        {
        }

        private async void OnStartup(object sender, StartupEventArgs e) 
        {
            var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c => { c.SetBasePath(appLocation); })
                .ConfigureServices(ConfigureServices)
                .Build();

            await _host.StartAsync();
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // App Host
            services.AddHostedService<ApplicationHostService>();

            // Activation Handlers

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Services
            services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
            services.AddSingleton<ISystemService, SystemService>();
            services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Views and ViewModels
            services.AddTransient<IShellWindow, ShellWindow>();
            services.AddTransient<ShellViewModel>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();


            // Configuration
            services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {

        }
    }
}
