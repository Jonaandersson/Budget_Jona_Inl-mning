#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.ViewModels;

namespace Budget_Jona_Inlämning;

public partial class App : Application
{
    private IHost? _host;
    private IServiceScope? _appScope;

    public IServiceProvider Services => this._host!.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed applying migrations: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Create a scope for application UI lifetime and resolve MainWindow from it
        this._appScope = this._host.Services.CreateScope();
        var main = this._appScope.ServiceProvider.GetRequiredService<MainWindow>();
        main.Show();

        // Load initial data via composed MainViewModel sequentially to avoid DbContext concurrency
        var vm = this._appScope.ServiceProvider.GetService<MainViewModel>();
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