using BusinessLayer.Concrete;
using DataAccesLayer.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.Controllers
{

    [AllowAnonymous]
    public class GalaryController : Controller
    {
        GalaryManager galaryManager = new GalaryManager(new EFGalaryDal());

        public IActionResult Index()
        {
            var data = galaryManager.TGetList();
            return View(data);
        }
    }
}
