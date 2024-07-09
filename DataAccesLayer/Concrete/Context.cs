using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccesLayer.Concrete
{
    public class Context : IdentityDbContext<AppUser,AppRole, int>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("server=77.245.159.27\\MSSQLSERVER2019;database=wellness_;user=Welness;password=&r481v7cI");
            //optionsBuilder.UseSqlServer("server=KOZLOW\\MSSQLSERVER01;database=WellnessSpa; integrated security=true;");
        }
        public DbSet<About> Abouts { get; set; }
        public DbSet<Home> Homes { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Galary> Galaries { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Blog> Blogs { get; set; }

    }
}
