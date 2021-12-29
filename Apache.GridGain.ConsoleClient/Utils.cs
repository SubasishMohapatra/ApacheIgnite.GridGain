/*
 * Copyright 2019 GridGain Systems, Inc. and Contributors.
 *
 * Licensed under the GridGain Community Edition License (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.gridgain.com/products/software/community-edition/gridgain-community-edition-license
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Apache.GridGain.ConsoleClient
{
    using System;
    using System.Collections.Generic;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Cache;
    using Apache.Ignite.Core.Deployment;
    using Apache.Ignite.Core.Discovery.Tcp;
    using Apache.Ignite.Core.Discovery.Tcp.Multicast;
    using Apache.Ignite.Core.Log;

    /// <summary>
    /// Common configuration and sample data.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Initializes the <see cref="Utils"/> class.
        /// </summary>
        static Utils()
        {
            // Only necessary during Ignite development.
            Environment.SetEnvironmentVariable("IGNITE_NATIVE_TEST_CLASSPATH", "true");
        }

        /// <summary>
        /// Gets the server node configuration.
        /// </summary>
        public static IgniteConfiguration GetServerNodeConfiguration()
        {
            // None of the options below are mandatory for the examples to work.
            // * Discovery and localhost settings improve startup time
            // * Logging options reduce console output
            return new IgniteConfiguration
            {
                Localhost = "127.0.0.1",
                DiscoverySpi = new TcpDiscoverySpi
                {
                    IpFinder = new TcpDiscoveryMulticastIpFinder
                    {
                        Endpoints = new[]
                        {
                            //"127.0.0.1:47500..47502"
                            "127.0.0.1:47500..47509"
                        }
                    }
                },
                JvmOptions = new[]
                {
                    "-DIGNITE_QUIET=true",
                    "-DIGNITE_PERFORMANCE_SUGGESTIONS_DISABLED=true"
                },
                Logger = new ConsoleLogger
                {
                    MinLevel = LogLevel.Error
                },
                PeerAssemblyLoadingMode = PeerAssemblyLoadingMode.CurrentAppDomain
            };
        }

        /// <summary>
        /// Gets the thick client node configuration.
        /// </summary>
        public static IgniteConfiguration GetClientNodeConfiguration()
        {
            return new IgniteConfiguration(GetServerNodeConfiguration())
            {
                ClientMode = true
            };
        }

        /// <summary>
        /// Gets the thin client node configuration.
        /// </summary>
        public static IgniteClientConfiguration GetThinClientConfiguration()
        {
            return new IgniteClientConfiguration
            {
                Endpoints = new[]
                {
                    "127.0.0.1"
                }
            };
        }

    }
}
