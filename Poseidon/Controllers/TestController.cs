using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Poseidon.Controllers
{
    public class TestController : Controller
    {
        //
        // GET: /Test/

        public ActionResult Index(int _param1, int _param2)
        {
            ViewBag.param1 = _param1;
            ViewBag.param2 = _param2;
            return View();
        }

    }
}
