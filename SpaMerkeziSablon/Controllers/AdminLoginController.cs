using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpaMerkeziSablon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaMerkeziSablon.Controllers
{
    public class AdminLoginController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AdminLoginController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Authorize]
        public IActionResult Index()
        {
            var values = _userManager.Users.ToList();
            return View(values);
        }

        [Authorize]
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SignUp(UserRegisterViewModel p, string password)
        {
            AppUser appUser = new AppUser()
            {
                UserName = p.UserName,
                name = p.name,
                Email = p.mail
            };
            if (string.IsNullOrEmpty(password))
            {
            }
            else
            {
                if (p.Password == p.ConfirmPassword)
                {
                    var result = await _userManager.CreateAsync(appUser, p.Password);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index","AdminLogin");
                    }
                    else
                    {
                        foreach (var item in result.Errors)
                        {
                            ModelState.AddModelError("", item.Description);
                        }
                    }
                }
            }
            return View(p);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(UserSignIn p)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(p.userName, p.userPassword, false, true);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "AdminLogin");
                }
                else
                {
                    return RedirectToAction("Login", "AdminLogin");
                }
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index","AdminLogin");
                }
                else
                {
                    // Hata mesajlarını işleme
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "AdminLogin");
        }
    }
}

