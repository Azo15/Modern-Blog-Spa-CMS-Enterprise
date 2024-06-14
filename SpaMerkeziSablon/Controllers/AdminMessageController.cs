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
    [Authorize]
    public class AdminMessageController : Controller
    {
        MessageManager messageManager = new MessageManager(new EFMessageDal());

        public IActionResult Index()
        {
            var values = messageManager.TGetList().OrderByDescending(m=> m.MessageDate).ToList();
            return View(values);
        }

        public IActionResult DeleteMessage(int id)
        {
            var values = messageManager.TGetByID(id);
            messageManager.TDelete(values);
            return RedirectToAction("Index", "AdminMessage");
        }
    }
}
