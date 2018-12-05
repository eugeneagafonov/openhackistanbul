using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubernetesApi.Models{
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