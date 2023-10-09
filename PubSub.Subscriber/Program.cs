using System;
using System.Threading.Tasks;
using Amazon.Runtime.CredentialManagement;
using MassTransit;
using Microsoft.Extensions.Hosting;
using PubSub.Messages;
using Serilog;

namespace PubSub.Subscriber
{
    class Program
    {
        static IHost ConfigureHost() {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => {
                    services.AddMassTransit(x =>
                        {
                            x.UsingAmazonSqs((_, config) =>
                            {
                                    const string region = "eu-west-1";
                                    const string profileName = "profileName";
                                    const string queueName = "queueName";
                                    const string keyString = "keyString";

                                    var profileChain = new CredentialProfileStoreChain();
                                    if (!profileChain.TryGetAWSCredentials(profileName, out var awsCredentials))
                                    {
                                        throw new InvalidOperationException("Could not retrieve AWS profile.");
                                    }

                                    config.Host(region, h =>
                                    {
                                        h.Credentials(awsCredentials);
                                    });

                                    var key = Convert.FromBase64String(keyString);
                                    config.UseEncryption(key);

                                    config.ReceiveEndpoint(queueName, e =>
                                    {
                                        e.UseMessageRetry(r => r.Immediate(5));

                                        e.Consumer(() => new Handler());
                                    });
                                });
                        });
                })
                .UseSerilog()
                .Build()

            return host;
        }

        static async Task Main()
        {
            var host = ConfigureHost();
            await host.RunAsync();
        }
    }

    class Handler : IConsumer<StringMessage>
    {
        public Task Consume(ConsumeContext<StringMessage> context)
        {
            Console.WriteLine("Got string: {0}", context.Message.Message);
            return Task.CompletedTask;
        }
    }
}
