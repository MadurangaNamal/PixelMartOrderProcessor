using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace Shared.Configuration;

public static class RabbitMqInstrumentation
{
    private static readonly ActivitySource ActivitySource = OpenTelemetryConfiguration.ActivitySource;
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    // Call when publishing a message
    public static Activity? StartPublishActivity(string queueName, object message)
    {
        var activity = ActivitySource.StartActivity(
            $"{queueName} publish",
            ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination", queueName);
            activity.SetTag("messaging.operation", "publish");
            activity.SetTag("messaging.message_id", Guid.NewGuid().ToString());
            activity.SetTag("messaging.message_type", message.GetType().Name);
        }

        return activity;
    }

    // Call when consuming a message
    public static Activity? StartConsumeActivity(string queueName, IDictionary<string, object>? headers)
    {
        // Extract trace context from message headers
        var parentContext = Propagator.Extract(
            default,
            headers,
            ExtractTraceContext);

        Baggage.Current = parentContext.Baggage;

        var activity = ActivitySource.StartActivity(
            $"{queueName} receive",
            ActivityKind.Consumer,
            parentContext.ActivityContext);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination", queueName);
            activity.SetTag("messaging.operation", "receive");
        }

        return activity;
    }

    // Inject trace context into message headers before publishing
    public static void InjectTraceContext(IDictionary<string, object> headers)
    {
        Propagator.Inject(
            new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
            headers,
            InjectTraceContext);
    }

    private static IEnumerable<string> ExtractTraceContext(IDictionary<string, object>? headers, string key)
    {
        if (headers != null && headers.TryGetValue(key, out var value))
        {
            if (value is byte[] bytes)
            {
                return new[] { System.Text.Encoding.UTF8.GetString(bytes) };
            }

            return new[] { value.ToString() ?? string.Empty };
        }

        return Enumerable.Empty<string>();
    }

    private static void InjectTraceContext(IDictionary<string, object> headers, string key, string value)
    {
        headers[key] = value;
    }
}
