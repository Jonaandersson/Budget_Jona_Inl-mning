#nullable enable
using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace Budget_Jona_Inlämning;



/// <summary>
/// Represents the entry point and main application class for the WPF budget management app, responsible for configuring
/// services, managing application lifetime, and initializing the main window.
/// </summary>
/// <remarks>This class sets up dependency injection, applies database migrations on startup, and manages the
/// lifetime scopes for application services and UI components. It overrides startup and exit events to ensure proper
/// initialization and cleanup of resources. Access application-wide services via the <see cref="Services"/>
/// property.</remarks>
public partial class App : Application
{
    private IHost? _host;
    private IServiceScope? _appScope;

    public IServiceProvider Services => this._host!.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var ci = new CultureInfo("sv-SE");
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;

        this._host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite("Data Source=budget.db");
                });

                services.AddScoped<ICategoryService, CategoryService>();
                services.AddScoped<ITransactionService, TransactionService>();
                services.AddScoped<IIncomeLossService, IncomeLossService>();

                services.AddSingleton<IDialogService, DialogService>();

                // LIST (scoped) viewmodels - own CRUD/listing
                services.AddScoped<CategoryViewModel>();         // category list + CRUD
                services.AddScoped<TransactionViewModel>();      // transaction list + CRUD
                services.AddScoped<IncomeLossViewModel>();       // income-loss list + CRUD

                // EDITOR (transient) viewmodels & factories (fresh instance per dialog)
                services.AddTransient<TransactionEditorViewModel>();
                services.AddTransient<Func<TransactionEditorViewModel>>(sp => () => ActivatorUtilities.CreateInstance<TransactionEditorViewModel>(sp));

                services.AddTransient<CategoryEditorViewModel>();
                services.AddTransient<Func<CategoryEditorViewModel>>(sp => () => ActivatorUtilities.CreateInstance<CategoryEditorViewModel>(sp));

                services.AddTransient<Func<IncomeLossViewModel>>(sp => () => ActivatorUtilities.CreateInstance<IncomeLossViewModel>(sp));

                // composition root
                services.AddScoped<MainViewModel>();

                services.AddScoped<MainWindow>();
            })

            .Build();

        this._host.Start();

        // Apply migrations
        try
        {
            using var scope = this._host.Services.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed applying migrations: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // a scope for application UI lifetime and resolve MainWindow from it
        this._appScope = this._host.Services.CreateScope();
        MainWindow main = this._appScope.ServiceProvider.GetRequiredService<MainWindow>();
        main.Show();

        // Load initial data via composed MainViewModel sequentially to avoid DbContext concurrency
        MainViewModel vm = this._appScope.ServiceProvider.GetService<MainViewModel>();
        if (vm is not null)
        {
            _ = Task.Run(async () => await vm.LoadAllAsync().ConfigureAwait(false));
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (this._appScope is not null)
        {
            this._appScope.Dispose();
            this._appScope = null;
        }

        if (this._host is not null)
        {
            this._host.StopAsync().GetAwaiter().GetResult();
            this._host.Dispose();
        }

        base.OnExit(e);
    }
}