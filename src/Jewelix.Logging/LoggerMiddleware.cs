using Microsoft.AspNetCore.Http;

using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Jewelix.Logging;

/// <summary>
/// Captures HTTP request/response metadata and, when Debug logging is enabled,
/// the bodies as well. Each request scope decorates every log event with the
/// correlation id (read from or assigned to the <c>X-Correlation-ID</c> header)
/// and pushes <c>ElapsedMs</c> onto <see cref="LogContext"/> around the summary
/// event so the configured outputTemplate can render it.
/// </summary>
public sealed partial class LoggerMiddleware
{
	private const string CorrelationIdHeader = "X-Correlation-ID";
	private const string CorrelationIdItem = "CorrelationId";
	private const int MaxBodyChars = 4_000;

	// Source-generated regex compiled at build time. Matches a sensitive JSON property
	// and replaces only its value, preserving the surrounding quotes.
	[GeneratedRegex(
		pattern: """("(?:password|token|secret|creditCard|apiKey)"\s*:\s*")[^"]*(")""",
		options: RegexOptions.IgnoreCase)]
	private static partial Regex SensitiveFieldRegex();

	private readonly RequestDelegate _next;

	/// <summary>
	/// Initialises the middleware with the next <see cref="RequestDelegate"/> in the pipeline.
	/// </summary>
	public LoggerMiddleware(RequestDelegate next)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
	}

	/// <summary>
	/// Middleware entry point. Assigns or propagates the correlation ID, delegates to
	/// the body-capture path when Debug logging is enabled, and emits an HTTP summary
	/// log event with <c>ElapsedMs</c> on the normal (non-Debug) path.
	/// </summary>
	public async Task Invoke(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		var stopwatch = Stopwatch.StartNew();

		var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var header)
			&& !string.IsNullOrWhiteSpace(header.ToString())
				? header.ToString()
				: Guid.NewGuid().ToString();

		context.Items[CorrelationIdItem] = correlationId;

		context.Response.OnStarting(() =>
		{
			context.Response.Headers[CorrelationIdHeader] = correlationId;
			return Task.CompletedTask;
		});

		using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
		using (LogContext.PushProperty("CorrelationId", correlationId))
		{
			if (Log.IsEnabled(LogEventLevel.Debug))
			{
				await InvokeWithBodyCaptureAsync(context, stopwatch).ConfigureAwait(false);
				return;
			}

			await _next(context).ConfigureAwait(false);
			stopwatch.Stop();

			// ElapsedMs is pushed onto LogContext (not the message template) so the
			// configured outputTemplate placeholder {ElapsedMs} renders it directly.
			// The summary message intentionally omits the literal "HTTP " prefix —
			// the outputTemplate provides it.
			using (LogContext.PushProperty("ElapsedMs", stopwatch.ElapsedMilliseconds))
			{
				Log.Information("{Method} {Path} responded {StatusCode}",
					context.Request.Method,
					context.Request.Path,
					context.Response.StatusCode);
			}
		}
	}

	/// <summary>
	/// Debug-level path: reads and logs the request body, swaps the response stream for
	/// a <see cref="MemoryStream"/> to capture the response body, then restores the
	/// original stream and copies the captured bytes back to the caller.
	/// </summary>
	private async Task InvokeWithBodyCaptureAsync(HttpContext context, Stopwatch stopwatch)
	{
		var requestBody = await ReadRequestBodyAsync(context).ConfigureAwait(false);
		if (!string.IsNullOrEmpty(requestBody))
		{
			Log.Debug("RequestBody={Body}", Sanitize(requestBody));
		}

		var originalResponseBody = context.Response.Body;
		var capturedBody = new MemoryStream();
		context.Response.Body = capturedBody;

		try
		{
			await _next(context).ConfigureAwait(false);

			stopwatch.Stop();

			capturedBody.Seek(0, SeekOrigin.Begin);
			var responseText = string.Empty;
			if (IsTextLike(context.Response.ContentType))
			{
				using var reader = new StreamReader(capturedBody, Encoding.UTF8, leaveOpen: true);
				responseText = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
				capturedBody.Seek(0, SeekOrigin.Begin);
			}

			using (LogContext.PushProperty("ElapsedMs", stopwatch.ElapsedMilliseconds))
			{
				if (!string.IsNullOrEmpty(responseText))
				{
					Log.Debug("ResponseBody={Body}", Sanitize(responseText));
				}

				Log.Debug("{Method} {Path} responded {StatusCode}",
					context.Request.Method,
					context.Request.Path,
					context.Response.StatusCode);
			}

			await capturedBody.CopyToAsync(originalResponseBody, context.RequestAborted).ConfigureAwait(false);
		}
		finally
		{
			// Restore the original stream first — exception path or otherwise — so that
			// upstream middleware (e.g. ExceptionHandler) writes to the real socket,
			// then dispose our buffer explicitly to keep CA2007 happy.
			context.Response.Body = originalResponseBody;
			await capturedBody.DisposeAsync().ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Reads the request body into a string, enabling buffering first so the body can
	/// be re-read by subsequent middleware. Returns <see cref="string.Empty"/> for
	/// non-text content types or bodies that exceed the logging threshold.
	/// </summary>
	private static async Task<string> ReadRequestBodyAsync(HttpContext context)
	{
		if (!IsTextLike(context.Request.ContentType))
		{
			return string.Empty;
		}

		if (context.Request.ContentLength is long length && length > MaxBodyChars * 4)
		{
			// A body that would just truncate to the same fingerprint isn't worth logging.
			return string.Empty;
		}

		context.Request.EnableBuffering();

		using var reader = new StreamReader(
			stream: context.Request.Body,
			encoding: Encoding.UTF8,
			detectEncodingFromByteOrderMarks: false,
			bufferSize: 1024,
			leaveOpen: true);

		var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
		context.Request.Body.Position = 0;
		return body;
	}

	/// <summary>
	/// Returns <see langword="true"/> when <paramref name="contentType"/> is a text-based
	/// media type (JSON, XML, form-encoded, or any <c>text/*</c> variant) that is safe to
	/// read as a UTF-8 string for logging.
	/// </summary>
	private static bool IsTextLike(string? contentType)
	{
		return !string.IsNullOrEmpty(contentType) && (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
			|| contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)
			|| contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
			|| contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Truncates <paramref name="text"/> to <paramref name="maxLength"/> characters and
	/// replaces the values of known sensitive JSON fields (password, token, secret,
	/// creditCard, apiKey) with <c>***MASKED***</c>. Safe to call with an empty string.
	/// </summary>
	internal static string Sanitize(string text, int maxLength = MaxBodyChars)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		var truncated = text.Length <= maxLength
			? text
			: string.Concat(text.AsSpan(0, maxLength), "...(truncated)");

		return SensitiveFieldRegex().Replace(truncated, "$1***MASKED***$2");
	}
}
