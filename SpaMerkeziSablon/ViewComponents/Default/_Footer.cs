using BusinessLayer.Concrete;
using DataAccesLayer.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.ViewsComponents.Default
{
    public class _Footer : ViewComponent
    {
        ContactManager contactManager = new ContactManager(new EFContactDal());

        public IViewComponentResult Invoke()
        {
            var data = contactManager.TGetList();
            return View(data);
        }
    }
}
