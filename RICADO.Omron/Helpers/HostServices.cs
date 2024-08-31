using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace RICADO.Omron.Helpers
{
    /// <summary>
    /// During app startup, call HostServices.Init to pass server provider instance.
    /// e.g.
    ///     var builder = Host.CreateApplicationBuilder();
    ///     builder.Services.AddSingleton<ILoggerFactory>(EdLogger.LoggerFactory.GetLoggerFactory());
    ///     var host = builder.Build();
    ///     host.StartAsync();
    ///     HostServices.Init(host.Services);
    /// </summary>
    public static class HostServices
    {
        private static IServiceProvider _services;
        private static ILogger _logger;

        public static void Init(IServiceProvider services)
        {
            _services = services;
        }

        internal static ILoggerFactory GetLoggingService() => _services?.GetService<ILoggerFactory>();

        internal static ILogger CreateLogger()
        {
            _logger ??= GetLoggingService()?.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name);
            return _logger;
        }

    }
}
