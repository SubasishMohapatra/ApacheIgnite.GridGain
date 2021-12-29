using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;
using Apache.Ignite.Core.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Apache.GridGain.ConsoleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Console.WriteLine("Hello");
            //Console.ReadLine();
            var config = new ConfigurationBuilder()
    .AddJsonFile("hosting.json", optional: true) //this is not needed, but could be useful
    .AddCommandLine(args)
    .Build();
            using IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((hostingContext, config) =>
              {
                  config.AddCommandLine(args);
              })
               .ConfigureServices((_, services) =>
               {
                   var serilogLogger = new LoggerConfiguration()
              .WriteTo.Console()
              .WriteTo.RollingFile("log.txt", shared: true)
              .CreateLogger();
                   services.AddLogging(builder =>
                   {
                       builder.SetMinimumLevel(LogLevel.Warning);
                       builder.AddSerilog(logger: serilogLogger, dispose: true);
                   });services.AddHostedService<BulkOperationsService>();
               });
    }
    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        var ignite = Ignition.Start(new IgniteConfiguration
    //        {
    //            DiscoverySpi = new TcpDiscoverySpi
    //            {
    //                IpFinder = new TcpDiscoveryStaticIpFinder
    //                {
    //                    Endpoints = new[] { "127.0.0.1:47500..47509" }
    //                },
    //                SocketTimeout = TimeSpan.FromSeconds(0.9)
    //            },
    //            IncludedEventTypes = EventType.CacheAll,
    //            JvmOptions = new[] { "-Xms1024m", "-Xmx1024m" },
    //            WorkDirectory = Environment.CurrentDirectory,
    //            ClientMode=true,
    //            JavaPeerClassLoadingEnabled=true                
    //        });

    //        var cache0 = ignite.GetOrCreateCache<object, object>(new CacheConfiguration
    //        {
    //            Name = Constants.Cache.Name,
    //            QueryEntities = new[]
    //                {
    //                    new QueryEntity
    //                    {
    //                        KeyType = typeof(int),
    //                        ValueTypeName = Constants.Cache.ValueTypeName,
    //                        Fields = new[]
    //                        {
    //                            new QueryField(NameField, typeof(string)),
    //                            new QueryField(CompanyIdField, typeof(int)),
    //                        },
    //                        Indexes = new[]
    //                        {
    //                            new QueryIndex(false, QueryIndexType.FullText, NameField),
    //                            new QueryIndex(false, QueryIndexType.Sorted, CompanyIdField)
    //                        }
    //                    },
    //                    new QueryEntity
    //                    {
    //                        KeyType = typeof(int),
    //                        ValueTypeName = CompanyType,
    //                        Fields = new[]
    //                        {
    //                            new QueryField(IdField, typeof(int)),
    //                            new QueryField(NameField, typeof(string))
    //                        }
    //                    }
    //                }
    //        });

    //        //Single exntry example below
    //        var cache = ignite.GetOrCreateCache<int, string>("my-cache");
    //        cache.Put(1, "Hello, World");
    //        Console.WriteLine(cache.Get(1));
    //    }
    //}
}
