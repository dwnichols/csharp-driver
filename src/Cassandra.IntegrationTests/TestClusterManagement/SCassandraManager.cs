using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cassandra.IntegrationTests.TestClusterManagement
{
    public class SCassandraManager
    {

        private static Process _scassandraProcess;

        public static void Start()
        {
            if (_scassandraProcess != null)
            {
                _scassandraProcess.Kill();
            }
            _scassandraProcess = new Process();
            var jarPath = Environment.GetEnvironmentVariable("SCASSANDRA_JAR");
            _scassandraProcess.StartInfo.FileName = "java";
            _scassandraProcess.StartInfo.Arguments = string.Format("-jar {0}", jarPath);
            _scassandraProcess.StartInfo.UseShellExecute = false;
            _scassandraProcess.StartInfo.CreateNoWindow = true;
            _scassandraProcess.StartInfo.RedirectStandardOutput = true;
            _scassandraProcess.StartInfo.RedirectStandardError = true;
#if !NETCORE
            _scassandraProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif
            var eventWaitHandler = new AutoResetEvent(false);
            var isReady = false;
            _scassandraProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (e.Data.Contains("ServerReady") && !isReady)
                    {
                        isReady = true;
                        eventWaitHandler.Set();
                    }
                }
            };
            _scassandraProcess.Start();

            _scassandraProcess.BeginOutputReadLine();
            _scassandraProcess.BeginErrorReadLine();

            eventWaitHandler.WaitOne(10000);
            if (!isReady)
                throw new Exception("SCassandra not started!");
        }

        public static void Stop()
        {
            if (_scassandraProcess != null)
            {
                _scassandraProcess.Kill();
            }
        }

        public static async Task SetupInitialConf()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8043");
                var json = "{\"when\":{\"query\":\"SELECT * FROM system.local WHERE key='local'\"}," +
                           "\"then\":{\"rows\":[{\"cluster_name\":\"custom cluster name\"," +
                           "\"partitioner\":\"org.apache.cassandra.dht.Murmur3Partitioner\"," +
                           "\"data_center\":\"dc1\",\"rack\":\"rc1\",\"tokens\":[\"1743244960790844724\"],\"release_version\":\"2.0.1\"}]," +
                           "\"result\":\"success\",\"column_types\":{\"tokens\":\"set<text>\"}}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/prime-query-single", content);
                response.EnsureSuccessStatusCode();
            }
        }

        public static async Task<int[]> GetListOfConnectedPorts()
        {
            //current/connections
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8043");
                var response = await client.GetAsync("/current/connections");
                if (response.IsSuccessStatusCode)
                {
                    var connections = await response.Content.ReadAsAsync<ConnectionsResponse>();
                    var ports = connections.connections.Select(x => x.port);
                    return ports.ToArray();
                }

            }
            return null;
        }

        public static async Task DropConnection(int port)
        {
            //current/connections/127.0.0.1/<port>
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8043");
                var response = await client.DeleteAsync(string.Format("/current/connections/127.0.0.1/{0}", port));
                response.EnsureSuccessStatusCode();
            }
        }

        public static async Task DisableConnectionListener()
        {
            //http://127.0.0.1:9043/current/listener
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8043");
                var response = await client.DeleteAsync("/current/listener");
                response.EnsureSuccessStatusCode();
            }
        }
        public static async Task EnableConnectionListener()
        {
            //http://127.0.0.1:9043/current/listener
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8043");
                var response = await client.PutAsync("/current/listener", null);
                response.EnsureSuccessStatusCode();
            }
        }
    }

    class ConnectionsResponse
    {
        public ConnectionPort[] connections { get; set; }
    }

    class ConnectionPort
    {
        public string host { get; set; }
        public int port { get; set; }
    }
}
