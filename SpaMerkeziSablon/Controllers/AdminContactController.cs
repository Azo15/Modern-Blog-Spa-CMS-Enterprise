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
    public class AdminContactController : Controller
    {
        ContactManager contactManager = new ContactManager(new EFContactDal());

        public IActionResult Index()
        {
            var values = contactManager.TGetList();
            return View(values);
        }

        public IActionResult DeleteContact(int id)
        {
            var values = contactManager.TGetByID(id);
            contactManager.TDelete(values);
            return RedirectToAction("Index", "AdminContact");
        }

        [HttpGet]
        public IActionResult UpdateContact(int id)
        {
            var values = contactManager.TGetByID(id);
            return View(values);
        }

        [HttpPost]
        public IActionResult UpdateContact(Contact contact)
        {
            contactManager.TUpdate(contact);
            return RedirectToAction("Index", "AdminContact");
        }

    }
}
