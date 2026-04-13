using InvoiceGeneratorFromWZ.Contracts.Repositories;
using InvoiceGeneratorFromWZ.Contracts.Services;
using InvoiceGeneratorFromWZ.Contracts.Settings;
using InvoiceGeneratorFromWZ.Infrastructure.Data;
using InvoiceGeneratorFromWZ.Infrastructure.Repositories;
using InvoiceGeneratorFromWZ.Infrastructure.Services;
using InvoiceGeneratorFromWZ.Service;
using InvoiceGeneratorFromWZ.Service.Constants;
using InvoiceGeneratorFromWZ.Service.Logging;
using InvoiceGeneratorFromWZ.Service.Services;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = ServiceConstants.ServiceName;
    })
    .UseSerilog((hostContext, _, loggerConfiguration) =>
    {
        loggerConfiguration.ConfigureServiceLogging(hostContext.Configuration);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

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
    .Build();

host.Run();