using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Jewelix.Logging;

/// <summary>
/// DI and pipeline registration for the Jewelix logging library.
/// </summary>
public static class LoggerExtensions
{
	/// <summary>
	/// The standard Serilog output template used by <see cref="UseJewelixLogger"/>.
	/// Includes timestamp, level, request/correlation IDs, elapsed milliseconds, and
	/// the structured HTTP message. Reference this constant when configuring additional
	/// sinks (e.g. file) in <c>appsettings.json</c> to keep all sinks consistent.
	/// </summary>
	public const string OutputTemplate =
		"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] [{ElapsedMs}ms] HTTP {Message:lj}{NewLine}{Exception}";

	/// <summary>
	/// Registers the Serilog-backed <see cref="ILogger{T}"/> adapter so the rest
	/// of the application can keep depending on Microsoft.Extensions.Logging while
	/// we control the underlying sink configuration. Idempotent: any prior
	/// <c>ILogger&lt;&gt;</c> registration is removed before our own is added.
	/// </summary>
	public static IServiceCollection AddJewelixLogger(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		return services
			.RemoveAll(typeof(ILogger<>))
			.AddSingleton(typeof(ILogger<>), typeof(SerilogLogger<>));
	}

	/// <summary>
	/// Configures the static <see cref="Log.Logger"/> from the application's
	/// <c>appsettings.json</c> (the <c>Serilog</c> section) and registers
	/// <see cref="LoggerMiddleware"/>. A Console sink using <see cref="OutputTemplate"/>
	/// is always wired in; additional sinks (e.g. File) should be configured via
	/// <c>appsettings.json</c>. The configuration source is the single source of truth
	/// for minimum levels — a missing or invalid <c>Serilog</c> section will surface as
	/// a configuration error at startup.
	/// </summary>
	public static IApplicationBuilder UseJewelixLogger(this IApplicationBuilder app, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(app);
		ArgumentNullException.ThrowIfNull(configuration);

		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(configuration)
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: OutputTemplate, formatProvider: System.Globalization.CultureInfo.InvariantCulture)
			.CreateLogger();

		return app.UseMiddleware<LoggerMiddleware>();
	}
}
