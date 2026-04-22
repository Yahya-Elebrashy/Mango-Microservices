using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private IConnection _connection;

        public MessageBus(string hostname = "localhost", string username = "guest", string password = "guest")
        {
            _hostname = hostname;
            _username = username;
            _password = password;
        }
        public async Task PublishMessage(object message, string queueName)
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostname,
                    UserName = _username,
                    Password = _password
                };
                _connection = await factory.CreateConnectionAsync();
            }

            using var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            );
        }
    }
}
