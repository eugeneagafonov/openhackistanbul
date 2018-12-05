using k8s;
using k8s.Models;
using MCServerStatus;
using MCServerStatus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KubernetesMonitor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting monitor....");

            var config = KubernetesClientConfiguration.InClusterConfig();
            IKubernetes client = new Kubernetes(config);

            while (true)
            {
                try
                {
                    //var ports = await GetPortsAsync(client);
                    //var minecraftPort = ports?.Where(p => p.Name == "minecraftport")?.SingleOrDefault();
                    //var minecraftRconPort = ports?.Where(p => p.Name == "minecraftrcon")?.SingleOrDefault();

                    var containers = await GetContainersAsync(client);

                    var tasks = new List<Task<MineCraftInfo>>();
                    foreach(var c in containers)
                    {
                        //int? nodePort = minecraftPort.NodePort;
                        //if (null != nodePort)
                        //{
                            Console.WriteLine($"Connecting to {c.PodName} {c.PodIP}:25565");
                            var t = GetMinecraftStatsAsync(c.PodName, c.PodIP, 25565);
                            tasks.Add(t);
                        //}
                    }

                    var results = await Task.WhenAll(tasks);

                    foreach(var r in results)
                    {
                        Console.WriteLine(
                            $"Realm={r.PodName}, Online={r.Online}, Max={r.Max}, Population={r.Population}");
                    }
                    Console.WriteLine($"Total={results.Select(r => r.Online).Sum()}, Capacity={results.Select(r => r.Max).Sum()}");


                    //await OutputKubernetesInfoAsync();
                }
                catch (Microsoft.Rest.HttpOperationException httpOperationException)
                {
                    var phrase = httpOperationException.Response.ReasonPhrase;
                    var content = httpOperationException.Response.Content;
                    Console.WriteLine(content);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                }

                System.Threading.Thread.Sleep(10000);
            }
        }

        private static async Task<MineCraftInfo> GetMinecraftStatsAsync(string podName, string address, short port)
        {
            try
            {
                var mcServerStatus = new MinecraftPinger(address, port);
                Status minecraftStatus = await mcServerStatus.PingAsync();
                return new MineCraftInfo {
                    Online = minecraftStatus.Players.Online,
                    Max = minecraftStatus.Players.Max,
                    Population = minecraftStatus.Players?.Sample?.Count ?? 0,
                    PodName = podName
                };
                //return $"players={minecraftStatus.Players.Online}, capacity={minecraftStatus.Players.Max}, population={minecraftStatus.Players?.Sample?.Count ?? 0}";
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception: {exception.Message}, Deails:{exception.StackTrace}");
            }

            return null;
        }

        private static async Task<List<V1ServicePort>> GetPortsAsync(IKubernetes client)
        {
            var services = await client.ListNamespacedServiceAsync("default");
            foreach (var item in services.Items)
            {
                Console.WriteLine(item.Metadata.Name);
                if (item.Metadata.Name.StartsWith("azure-vote-front"))
                {
                    var result = new List<V1ServicePort>();
                    foreach (var port in item.Spec.Ports)
                    {
                        result.Add(port);
                    }
                    return result;
                }
            }
            return null;
        }

        private static async Task<List<ContainerInfo>> GetContainersAsync(IKubernetes client)
        {
            var result = new List<ContainerInfo>();

            var list = await client.ListNamespacedPodAsync("default");
            foreach (var item in list.Items)
            {
                if (item.Metadata.Name.StartsWith("azure-vote-front"))
                {
                    var podContainerStatuses = new Dictionary<string, V1ContainerStatus>();

                    foreach(var status in item.Status.ContainerStatuses)
                    {
                        podContainerStatuses.Add(status.Name, status);
                    }

                    string podName = item.Metadata.Name;
                    string podIP = item.Status.PodIP;
                    string hostIP = item.Status.HostIP;

                    foreach (var container in item.Spec.Containers)
                    {
                        var c = new ContainerInfo();
                        c.PodName = podName;
                        c.PodIP = podIP;
                        c.HostIP = hostIP;

                        string containerID = podContainerStatuses.ContainsKey(container.Name) ?
                            podContainerStatuses[container.Name]?.ContainerID : null;
                        c.RunningContainerID = containerID;

                        foreach (var port in container.Ports)
                        {
                            var ports = new List<ContainerPortInfo>();
                            ports.Add(new ContainerPortInfo() {
                                ContainerPort = port.ContainerPort,
                                PortName = port.Name,
                                HostIP = port.HostIP,
                                HostPort = port.HostPort
                            });
                        }
                        result.Add(c);
                    }
                }
            }

            return result;
        }

        private static async Task OutputKubernetesInfoAsync()
        {
            try
            {
                Console.WriteLine("Starting monitor....");

                var config = KubernetesClientConfiguration.InClusterConfig();
                IKubernetes client = new Kubernetes(config);
                Console.WriteLine("Starting Request!");

                var list = client.ListNamespacedPod("default");
                foreach (var item in list.Items)
                {
                    Console.WriteLine(item.Metadata.Name);
                    if (item.Metadata.Name.StartsWith("azure-vote-front"))
                    {
                        Console.WriteLine($"Found the pod {item.Metadata.Name}");
                        Console.WriteLine($"Pod's hostname is {item.Spec.Hostname}");
                        //Console.WriteLine($"pod IP address: {item.Spec.HostAliases.Select(a => a.Ip).Aggregate((a, b) => $"{a};{b}").Trim(';')}");
                        string nodeName = item.Spec.NodeName;
                        var node = client.ListNode().Items.SingleOrDefault(n => n.Metadata.Name == nodeName);
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

                var services = client.ListNamespacedService("default");
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
                foreach (var item in client.ListNamespacedDeployment("default").Items)
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
    }

    public class ContainerInfo
    {
        public string PodName { get; set; }

        public string PodIP { get; set; }

        public string HostIP { get; set; }

        public string RunningContainerID { get; set; }

        public ContainerPortInfo[] Ports { get; set; }
    }

    public class ContainerPortInfo
    {
        public string PortName { get; set; }
        public int ContainerPort { get; set; }

        public string HostIP { get; set; }
        public int? HostPort { get; set; }
    }

    public class MineCraftInfo
    {
        public int Online { get; set; }

        public int Max { get; set; }

        public int Population { get; set; }

        public string PodName { get; set; }
    }

}
