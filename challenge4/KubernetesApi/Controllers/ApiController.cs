using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubernetesApi.Controllers
{
    [Route("/api")]
    public class ApiController : Controller
    {
        private IKubernetes _client;
        public ApiController(IKubernetes client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IActionResult> Get()
        {
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

            return Ok(result);
        }

        private void DebugOutput()
        {
            try
            {
                Console.WriteLine("Starting Request!");

                var list = _client.ListNamespacedPod("default");
                foreach (var item in list.Items)
                {
                    Console.WriteLine(item.Metadata.Name);
                    if (item.Metadata.Name.StartsWith("azure-vote-front"))
                    {
                        Console.WriteLine($"Found the pod {item.Metadata.Name}");
                        Console.WriteLine($"Pod's hostname is {item.Spec.Hostname}");
                        //Console.WriteLine($"pod IP address: {item.Spec.HostAliases.Select(a => a.Ip).Aggregate((a, b) => $"{a};{b}").Trim(';')}");
                        string nodeName = item.Spec.NodeName;
                        var node = _client.ListNode().Items.SingleOrDefault(n => n.Metadata.Name == nodeName);
                        if (null != node)
                        {
                            Console.WriteLine($"Pod's node name is {node.Metadata.Name}");
                            foreach (var address in node.Status.Addresses)
                            {
                                Console.WriteLine($"    - address type:{address.Type} value:{address.Address}");
                            }
                        }

                        Console.WriteLine("Enumerating containers!");
                        foreach (var container in item.Spec.Containers)
                        {
                            Console.WriteLine($"{container.Name} has following ports: ");
                            foreach (var port in container.Ports)
                            {
                                Console.WriteLine($"    - {port.Name},{port.ContainerPort} - {port.HostIP}:{port.HostPort}");
                            }
                        }
                    }
                }
                if (list.Items.Count == 0)
                {
                    Console.WriteLine("Empty!");
                }

                var services = _client.ListNamespacedService("default");
                foreach (var item in services.Items)
                {
                    Console.WriteLine(item.Metadata.Name);
                    if (item.Metadata.Name.StartsWith("azure-vote-front"))
                    {
                        Console.WriteLine($"Found the service {item.Metadata.Name}");
                        foreach (var port in item.Spec.Ports)
                        {
                            Console.WriteLine($"    - port Name:{port.Name} Port:{port.Port} NodePort:{port.NodePort} TargetPort:{port.TargetPort.Value}");

                        }
                    }
                }

                Console.WriteLine("ListNamespacedDeployment!");
                foreach (var item in _client.ListNamespacedDeployment("default").Items)
                {
                    Console.WriteLine($"Name {item.Metadata.Name}");
                    foreach (var label in item.Spec.Template.Metadata.Labels)
                    {
                        Console.WriteLine($"Label {label.Key} = {label.Value}");
                    }
                    foreach (var container in item.Spec.Template.Spec.Containers)
                    {
                        Console.WriteLine($"Image {container.Image}");
                    }
                }
            }
            catch (Microsoft.Rest.HttpOperationException httpOperationException)
            {
                var phrase = httpOperationException.Response.ReasonPhrase;
                var content = httpOperationException.Response.Content;
                Console.WriteLine(content);
            }
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

    public class EndpointInfo
    {
        public string name { get; set; }

        public MinecraftEndpointInfo endpoints { get; set; }
    }

    public class MinecraftEndpointInfo
    {
        public string minecraft { get; set; }

        public string rcon { get; set; }
    }

    public class PortInfo
    {
        public string IP { get; set; }

        public string ServiceName { get; set; }

        public List<V1ServicePort> Ports { get; set; }
    }
}