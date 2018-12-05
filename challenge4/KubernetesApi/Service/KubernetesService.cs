using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubernetesApi.Models;

namespace KubernetesApi.Service
{
    public class KubernetesService {
        private IKubernetes _client;
        public KubernetesService(IKubernetes client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<List<EndpointInfo>> getContainerEndpoints() {
            var result = new List<EndpointInfo>();
            var ports = await GetPortsAsync(_client);

            foreach(var p in ports)
            {
                var ep = new EndpointInfo();
                ep.name = p.ServiceName;

                var minecraftPort = p?.Ports?.Where(x => x.Name == "minecraftport")?.SingleOrDefault();
                var minecraftRconPort = p?.Ports?.Where(x => x.Name == "minecraftrcon")?.SingleOrDefault();

                ep.endpoints = new MinecraftEndpointInfo
                {
                    minecraft = $"{p.IP}:{minecraftPort.Port}",
                    rcon = $"{p.IP}:{minecraftRconPort.Port}"
                };

                result.Add(ep);
            }

            return result;
        }

        private static async Task<List<PortInfo>> GetPortsAsync(IKubernetes client)
        {
            var services = await client.ListNamespacedServiceAsync("default");
            var result = new List<PortInfo>();
            foreach (var item in services.Items)
            {
                if (item.Metadata.Name.StartsWith("azure-vote-front"))
                {
                    var pi = new PortInfo();
                    pi.ServiceName = item.Metadata.Name;
                    pi.IP = item.Spec.LoadBalancerIP;
                    pi.Ports = new List<V1ServicePort>();
                    foreach (var port in item.Spec.Ports)
                    {
                        pi.Ports.Add(port);
                    }
                    result.Add(pi);
                }
            }
            return result;
        }
    }
}