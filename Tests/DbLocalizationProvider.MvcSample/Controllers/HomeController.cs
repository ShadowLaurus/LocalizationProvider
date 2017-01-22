using System.Globalization;
using System.Threading;
using System.Web.Mvc;
using DbLocalizationProvider.MvcSample.Models;
using System.Collections.Generic;
using System;

namespace DbLocalizationProvider.MvcSample.Controllers {
    public class HomeController : Controller {
        public ActionResult Index(string l) {
            if (!string.IsNullOrEmpty(l)) {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(l ?? string.Empty);
            }

            return View(new HomeViewModel());
        }

        [HttpPost]
        public ActionResult Index(HomeViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            return View(new HomeViewModel());
        }
        public ActionResult List(string l) {
            if (!string.IsNullOrEmpty(l)) {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(l ?? string.Empty);
            }

            List<HomeViewModel> list = new List<HomeViewModel>();

            for (int i = 0; i < 1000; i++) {
                list.Add(new HomeViewModel() {
                    Message = Guid.NewGuid().ToString(),
                    Username = i.ToString(),
                    Date = DateTime.Now,
                    Number = i,
                    Address = new Address() { Street = Guid.NewGuid().ToString() }
                });
            }

            return View(list);
        }
    }
}
