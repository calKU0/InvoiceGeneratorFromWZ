using InvoiceGeneratorFromWZ.Contracts.Repositories;
using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Contracts.Settings;
using InvoiceGeneratorFromWZ.Infrastructure.Data;
using InvoiceGeneratorFromWZ.Infrastructure.Repositories;
using InvoiceGeneratorFromWZ.Infrastructure.Services;
using InvoiceGeneratorFromWZ.Service;
using InvoiceGeneratorFromWZ.Service.Constants;
using InvoiceGeneratorFromWZ.Service.Services;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = ServiceConstants.ServiceName;
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        var logsExpirationDays = Convert.ToInt32(configuration["AppSettings:LogsExpirationDays"]);
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: logsExpirationDays,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .CreateLogger();

        // Configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<List<WZGenerationTimes>>(configuration.GetSection("WZGenerationTimes"));
        services.Configure<XlApiSettings>(configuration.GetSection("XlApiSettings"));

        // Database context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<IDbExecutor>(sp => new DapperDbExecutor(connectionString));

        // Repositories
        services.AddScoped<IDocumentRespository, DocumentRepository>();

        // Services
        services.AddSingleton<IXlApiService, XlApiService>();
        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();

        // Background worker
        services.AddHostedService<Worker>();

        // Host options
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));
    })
    .UseSerilog()
    .Build();

host.Run();