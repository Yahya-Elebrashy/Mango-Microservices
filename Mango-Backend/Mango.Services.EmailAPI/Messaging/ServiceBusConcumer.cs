using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;

namespace Mango.Services.EmailAPI.Messaging
{
    public class ServiceBusConsumer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IConnection _connection;
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;

        // Multiple queues
        private readonly List<string> _queueNames;
        private readonly List<IChannel> _channels;

        public ServiceBusConsumer(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;

            // Read from appsettings.json
            _hostname = _configuration.GetValue<string>("RabbitMQ:Hostname") ?? "localhost";
            _username = _configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
            _password = _configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

            // Read multiple queue names
            _queueNames = new List<string>
            {
                _configuration.GetValue<string>("RabbitMQ:EmailCartQueue") ?? "emailcartqueue",
                _configuration.GetValue<string>("RabbitMQ:OrderCreatedQueue") ?? "OrderCreatedQueue"
            };

            _channels = new List<IChannel>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostname,
                UserName = _username,
                Password = _password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            // Listen to all queues
            foreach (var queueName in _queueNames)
            {
                await StartListeningToQueue(queueName, cancellationToken);
            }
        }

        private async Task StartListeningToQueue(string queueName, CancellationToken cancellationToken)
        {
            var channel = await _connection.CreateChannelAsync();
            _channels.Add(channel);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, args) =>
            {
                await ProcessMessage(queueName, channel, args);
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken
            );
        }

        private async Task ProcessMessage(string queueName, IChannel channel, BasicDeliverEventArgs args)
        {
            try
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    // Handle different message types based on queue name
                    if (queueName.Contains("emailcart", StringComparison.OrdinalIgnoreCase))
                    {
                        var cartDto = JsonSerializer.Deserialize<CartDto>(message);
                        if (cartDto != null)
                        {
                            await emailService.EmailCartAndLog(cartDto);
                        }
                    }
                    else if (queueName.Contains("ordercreated", StringComparison.OrdinalIgnoreCase))
                    {
                        var rewardsDto = JsonSerializer.Deserialize<RewardsDto>(message);
                        if (rewardsDto != null)
                        {
                            await emailService.LogOrderPlaced(rewardsDto);
                        }
                    }
                }

                await channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message from {queueName}: {ex.Message}");
                await channel.BasicNackAsync(args.DeliveryTag, false, true);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var channel in _channels)
            {
                if (channel != null)
                {
                    await channel.CloseAsync(cancellationToken);
                    channel.Dispose();
                }
            }

            if (_connection != null)
            {
                await _connection.CloseAsync(cancellationToken);
                _connection.Dispose();
            }
        }
    }
}
