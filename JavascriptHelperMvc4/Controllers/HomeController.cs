using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JavascriptHelperMvc4.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult WithHelper()
        {
            return View();
        }
		public ActionResult WithoutHelper()
		{
			return View();
		}
	}
}
