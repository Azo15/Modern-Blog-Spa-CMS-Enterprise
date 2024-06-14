using BusinessLayer.Concrete;
using DataAccesLayer.Concrete;
using DataAccesLayer.EntityFramework;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.Controllers
{

    [AllowAnonymous]
    public class ServiceController : Controller
    {
        ServiceManager _serviceManager = new ServiceManager(new EFServiceDal());

        public IActionResult Index()
        {
            var data = _serviceManager.TGetList();
            return View(data);
        }

        [HttpGet]
        public IActionResult ServiceDetails(int id)
        {
            var values = _serviceManager.TGetByID(id);
            if (values == null)
            {
                return NotFound();
            }
            return View(values);
        }

        [HttpPost]
        public IActionResult ServiceDetails(Service s)
        {
            return View();
        }
    }
}
