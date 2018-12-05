using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KubernetesApi.Models;
using KubernetesApi.Service;
using k8s;
using k8s.Models;

namespace KubernetesApi.Controllers
{
    public class AdminController : Controller
    {
        private IKubernetes _client;
        public AdminController(IKubernetes client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }
        public IActionResult Index()
        {
             KubernetesService ks = new KubernetesService(_client);
            var result = ks.getContainerEndpoints();

            return View(result);
        }
    }
}
