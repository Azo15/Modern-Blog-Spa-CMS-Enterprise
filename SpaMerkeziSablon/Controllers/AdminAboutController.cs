using BusinessLayer.Concrete;
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
    [Authorize]
    public class AdminAboutController : Controller
    {
        AboutManager aboutManager = new AboutManager (new EFAboutDal());

        public IActionResult Index()
        {
            var values = aboutManager.TGetList();
            return View(values);
        }

        public IActionResult DeleteAbout(int id)
        {
            var values = aboutManager.TGetByID(id);
            aboutManager.TDelete(values);
            return RedirectToAction("Index", "AdminAbout");
        }

        [HttpGet]
        public IActionResult CreateAbout()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateAbout(About about)
        {
            aboutManager.TAdd(about);
            return RedirectToAction("Index", "AdminAbout");
        }

        [HttpGet]
        public IActionResult UpdateAbout(int id)
        {
            var values = aboutManager.TGetByID(id);
            return View(values);
        }

        [HttpPost]
        public IActionResult UpdateAbout(About about)
        {
            aboutManager.TUpdate(about);
            return RedirectToAction("Index", "AdminAbout");
        }
    }
}
