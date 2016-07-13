using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using System.Configuration;
using Poseidon.Models;
using MongoDB.Driver.Builders;

namespace Poseidon.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public HomeController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}
