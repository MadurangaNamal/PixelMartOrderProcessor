namespace Shared.Constants;

public static class AppConstants
{
    public const string MigrationsAssembly = "PixelMartOrderProcessor";
    public const string ApplicationName = "PixelMartOrderProcessor";

    public static class Configuration
    {
        public const string DbPassword = "DB_PASSWORD";
        public const string DefaultConnection = "DefaultConnection";
        public const string DbPasswordPlaceholder = "{DB_PASSWORD}";
        public const string WorkerName = "WorkerName";
    }

    public static class RabbitMq
    {
        public const string Host = "RabbitMq:Host";
        public const string Port = "RabbitMq:Port";
        public const string Username = "RabbitMq:Username";
        public const string Password = "RabbitMq:Password";
        public const string OrderPlacedQueue = "RabbitMq:OrderPlacedQueue";
        public const string InventoryQueue = "RabbitMq:InventoryQueue";
        public const string EmailQueue = "RabbitMq:EmailQueue";

        public const string DefaultHost = "localhost";
        public const string DefaultPort = "5672";
        public const string DefaultUsername = "guest";
        public const string DefaultPassword = "guest";
        public const string DefaultOrderPlacedQueue = "order-placed-queue";
        public const string DefaultInventoryQueue = "inventory-queue";
        public const string DefaultEmailQueue = "email-queue";
    }

    public static class HealthChecks
    {
        public const string Database = "database";
        public const string RabbitMq = "rabbitmq";
        public const string Worker = "worker";
        public const string RabbitMqCustom = "rabbitmq-custom";
        public const string PostgresConnection = "postgres-connection";
        public const string PaymentWorker = "payment-worker";
        public const string InventoryWorker = "inventory-worker";
        public const string EmailWorker = "email-worker";

        public static class Tags
        {
            public const string Db = "db";
            public const string Sql = "sql";
            public const string Postgres = "postgres";
            public const string Messaging = "messaging";
            public const string RabbitMq = "rabbitmq";
            public const string Worker = "worker";
            public const string Remote = "remote";
            public const string Ready = "ready";
        }

        public static class Paths
        {
            public const string Health = "/health";
            public const string Ready = "/health/ready";
            public const string Live = "/health/live";
            public const string Ui = "/health-ui";
            public const string UiApi = "/health-ui-api";
        }
    }

    public static class Workers
    {
        public const string Payment = "PaymentWorker";
        public const string Inventory = "InventoryWorker";
        public const string Email = "EmailWorker";
    }

    public static class Cors
    {
        public const string AllowAll = "AllowAll";
    }

    public static class Api
    {
        public const string IdempotencyKeyHeader = "Idempotency-Key";
        public const string IdempotencyKeyColumn = "idempotency_key";

        public static class Messages
        {
            public const string IdempotencyKeyRequired = "Idempotency-Key header is required";
            public const string OrderAlreadyExists = "Order already exists (idempotent response)";
            public const string OrderPlacedSuccessfully = "Order placed successfully. Processing...";
            public const string ErrorProcessingOrder = "Error processing order";
            public const string ErrorPlacingOrder = "An error occurred while placing the order";
        }
    }
}
