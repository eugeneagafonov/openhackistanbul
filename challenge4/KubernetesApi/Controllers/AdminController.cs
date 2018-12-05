using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KubernetesApi.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            var result = new List<EndpointInfo>();
            result.Add(new EndpointInfo
            {
                name = "Test",
                endpoints = new MinecraftEndpointInfo
                {
                    minecraft = "ip:port1",
                    rcon = "ip:port2"
                }
            });
            result.Add(new EndpointInfo
            {
                name = "Test2",
                endpoints = new MinecraftEndpointInfo
                {
                    minecraft = "ip:port1",
                    rcon = "ip:port2"
                }
            });

            return View(result);
        }
    }
}
