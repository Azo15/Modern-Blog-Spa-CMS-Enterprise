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
    [AllowAnonymous]
    public class ContactController : Controller
    {
        MessageManager messageManager = new MessageManager(new EFMessageDal());

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Message c)
        {
            c.MessageDate = DateTime.Parse(DateTime.Now.ToShortTimeString());
            messageManager.TAdd(c);
            return View();
        }
    }
}
