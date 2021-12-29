using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Eviction;
using Apache.Ignite.Core.Configuration;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;
using Apache.Ignite.Core.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Apache.GridGain.ConsoleClient
{
    public class BulkOperationsService : BackgroundService
    {
        private readonly ILogger<BulkOperationsService> _logger;
        private IConfiguration _configuration;

        public BulkOperationsService(ILogger<BulkOperationsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var ignite = Ignition.Start(new IgniteConfiguration
            {
                DiscoverySpi = new TcpDiscoverySpi
                {
                    IpFinder = new TcpDiscoveryStaticIpFinder
                    {
                        Endpoints = new[] { "127.0.0.1:47500..47509" }
                    },
                    SocketTimeout = TimeSpan.FromSeconds(0.9)
                },
                IncludedEventTypes = EventType.CacheAll,
                JvmOptions = new[] { "-Xms1024m", "-Xmx1024m" },
                WorkDirectory = Environment.CurrentDirectory,
                ClientMode = true,
                JavaPeerClassLoadingEnabled = true,
                DataStorageConfiguration = new DataStorageConfiguration
                {
                    DefaultDataRegionConfiguration = new DataRegionConfiguration
                    {
                        Name = "Default_Region",
                        InitialSize = 1000 * 1024 * 1024
                    },
                    //DataRegionConfigurations = new[]
                    // {
                    //    new DataRegionConfiguration
                    //    {
                    //        Name = "40MB_Region_Eviction",
                    //        InitialSize = 20 * 1024 * 1024,
                    //        MaxSize = 40 * 1024 * 1024,
                    //        PageEvictionMode =Ignite.Core.Configuration.DataPageEvictionMode.Random2Lru
                    //    },
                    //    new DataRegionConfiguration
                    //    {
                    //        Name = "30MB_Region_Swapping",
                    //        InitialSize = 15 * 1024 * 1024,
                    //        MaxSize = 30 * 1024 * 1024,
                    //        SwapPath = "/path/to/swap/file"
                    //    }
                    //}
                }
                //,CacheConfiguration = new[]
                //{
                //    new CacheConfiguration
                //    {
                //        Name = "SampleCache",
                //        DataRegionName="40MB_Region_Eviction"
                //    }
                //}
            });
            var cache0 = ignite.GetOrCreateCache<object, object>(new CacheConfiguration
            {
                Name = Constants.Cache.Name,
                QueryEntities = new[]
                   {
                                new QueryEntity
                                {
                                    KeyType = typeof(int),
                                    ValueTypeName = Constants.Cache.MaintenanceValueTypeName,
                                    Fields = new[]
                                    {
                                        new QueryField(Constants.Cache.TypeFieldName, typeof(string)),
                                        new QueryField(Constants.Cache.DocIdFieldName, typeof(int)),
                                        new QueryField(Constants.Cache.JSonDataFieldName, typeof(string)),
                                    },
                                    Indexes = new[]
                                    {
                                        new QueryIndex(false, QueryIndexType.Sorted, Constants.Cache.DocIdFieldName),
                                        new QueryIndex(false, QueryIndexType.FullText, Constants.Cache.JSonDataFieldName),
                                    }
                                }
                            }
            });
            // Switch to binary mode to work with data in serialized form.
            var cache = cache0.WithKeepBinary<int, IBinaryObject>();
            while (true)
            {
                cache.Clear();
                Console.WriteLine("Cache cleared");
                Console.WriteLine("Press any key if you want to clear the cache, else ESC to quit program");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                    break;
            }
            await Task.Delay(1);
            Console.WriteLine(Environment.NewLine + "End of program");
            //await WriteJsonDocsInParallelAsync(cache);
            //await ReadJsonDocsInParallelAsync(cache);
        }


        private async Task WriteJsonDocsInParallelAsync(ICache<int, IBinaryObject> cache)
        {
            IBinary binary = cache.Ignite.GetBinary();
            var maxRecords = _configuration.GetValue<int>("CacheKey:RecordCount");
            var cacheUpperValue = _configuration.GetValue<int>("CacheKey:UpperValue") * maxRecords;
            JObject jObject;
            var jsonPath = @"MaintenanceDomain.json";
            using (StreamReader file = File.OpenText(jsonPath))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    jObject = (JObject)JToken.ReadFrom(reader);
                    jObject.Add("type", "maintenance");
                }
            }
            var source = Enumerable.Range(cacheUpperValue - (maxRecords - 1), maxRecords).ToArray();
            var stopWatch = new Stopwatch();
            int ctr = 0;
            stopWatch.Start();
            await source.ParallelForEachAsync(
                async item =>
                {

                    var jObj = jObject.ToObject<JObject>();
                    jObj.Add("docId", item);
                    var attempts = 5; // eg 5
                    var isError = false;
                    while (attempts-- > 0)
                    {
                        try
                        {
                            cache[item] = binary.GetBuilder(Constants.Cache.MaintenanceValueTypeName)
                    .SetField(Constants.Cache.DocIdFieldName, item)
                    .SetField(Constants.Cache.TypeFieldName, "maintenance")
                    .SetField(Constants.Cache.JSonDataFieldName, jObj.ToString())
                    .Build();
                            _logger.LogInformation($"inserted record {item}");
                            await Task.Delay(1);
                            isError = false;
                            Interlocked.Increment(ref ctr);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.ToString()); ;
                            isError = true;
                        }
                        if (isError)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                        else
                            break;
                    }
                },
                Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0))
            );
            stopWatch.Stop();
            _logger.LogWarning($"Time elapsed to insert {ctr} records out of {maxRecords} in miliseconds {stopWatch.Elapsed.TotalMilliseconds}");
        }

        /// <summary>
        /// Reads binary object fields and modifies them.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private async Task ReadJsonDocsInParallelAsync(ICache<int, IBinaryObject> cache)
        {
            IBinary binary = cache.Ignite.GetBinary();
            var maxRecords = _configuration.GetValue<int>("CacheKey:RecordCount");
            var cacheUpperValue = _configuration.GetValue<int>("CacheKey:UpperValue") * maxRecords;
            var source = Enumerable.Range(cacheUpperValue - (maxRecords - 1), maxRecords).ToArray();
            var stopWatch = new Stopwatch();
            int ctr = 0;
            stopWatch.Start();
            await source.ParallelForEachAsync(
               async item =>
               {
                   var attempts = 5; // eg 5
                   var isError = false;
                   while (attempts-- > 0)
                   {
                       try
                       {
                           var maintenanceObj = cache[item];
                           var jObj = maintenanceObj.GetField<string>(Constants.Cache.JSonDataFieldName);
                           var docId = maintenanceObj.GetField<int>(Constants.Cache.DocIdFieldName);
                           var type = maintenanceObj.GetField<string>(Constants.Cache.TypeFieldName);
                           _logger.LogInformation($"read record {item}");
                           await Task.Delay(1);
                           isError = false;
                           Interlocked.Increment(ref ctr);
                       }
                       catch (Exception ex)
                       {
                           _logger.LogError(ex.ToString()); ;
                           isError = true;
                       }
                       if (isError)
                       {
                           await Task.Delay(100);
                           continue;
                       }
                       else
                           break;
                   }
               },
               Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0))
           );
            stopWatch.Stop();
            _logger.LogWarning($"Time elapsed to read {ctr} of {maxRecords} records in miliseconds {stopWatch.Elapsed.TotalMilliseconds}");
        }

    }
}
