using System;
using Amazon.Runtime.CredentialManagement;
using MassTransit;
using PubSub.Messages;

namespace PubSub.Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.Factory.CreateUsingAmazonSqs(x =>
            {
                const string region = "eu-west-1";
                const string profileName = "profileName";
                const string topicName = "topicName";
                const string keyString = "keyString";

                var profileChain = new CredentialProfileStoreChain();
                if (!profileChain.TryGetAWSCredentials(profileName, out var awsCredentials))
                {
                    throw new InvalidOperationException("Could not retrieve AWS profile.");
                }

                x.Host(region, h =>
                {
                    h.Credentials(awsCredentials);
                });

                var key = Convert.FromBase64String(keyString);
                x.UseEncryption(key);

                x.Message<StringMessage>(cfg =>
                {
                    cfg.SetEntityName(topicName);
                });
            });

            bus.StartAsync().Wait();

            while (true)
            {
                Console.WriteLine("a) Publish string");
                Console.WriteLine("q) Quit");

                var keyChar = char.ToLower(Console.ReadKey(true).KeyChar);

                switch (keyChar)
                {
                    case 'a':
                        bus.Publish(new StringMessage("Hello there, I'm a publisher!")).Wait();
                        Console.WriteLine("Published");
                        break;

                    default:
                        Console.WriteLine("There's no option ({0})", keyChar);
                        break;
                }
            }
        }
    }

}
