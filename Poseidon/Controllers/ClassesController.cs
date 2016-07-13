using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using System.Configuration;
using Poseidon.Models;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Poseidon.Controllers
{
    public class ClassesController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public ClassesController()
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
