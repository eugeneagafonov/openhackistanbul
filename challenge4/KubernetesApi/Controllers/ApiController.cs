using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubernetesApi.Controllers
{
    [Route("/api")]
    public class ApiController : Controller
    {
        public IActionResult Get()
        {
            string[] result = new string[] { "one", "two", "three" };

            try
            {
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

            return Ok(result);
        }
    }
}