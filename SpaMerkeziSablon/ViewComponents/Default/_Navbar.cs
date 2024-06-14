using BusinessLayer.Concrete;
using DataAccesLayer.Concrete;
using DataAccesLayer.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.ViewComponents.Default
{
    public class _Navbar: ViewComponent
    {
        HomeManager homeManager = new HomeManager(new EFHomeDal());

        public IViewComponentResult Invoke()
        {
            var data = homeManager.TGetList();
            return View(data);
        }
    }
}
