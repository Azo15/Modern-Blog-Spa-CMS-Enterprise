using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.ViewComponents.Default
{
    public class _RezervasyonButton: ViewComponent
    {
        private readonly Context _context;

        public _RezervasyonButton(Context context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _context.Homes.ToListAsync();
            return View(data);
        }
    }
}
