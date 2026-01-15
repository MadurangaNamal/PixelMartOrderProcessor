namespace Shared.Helpers;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string queueName, T message);
}
