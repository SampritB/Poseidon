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
    [Authorize]
    public class TagsController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public TagsController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);
        }
        //
        // GET: /Tag/

        // design: index shows images and labels of tags with a filter bar (filter bar will filter the list of images shown)
        // clicking on an image will bring you to the examples page
        // examples page shows a list of all the labels for that tag
        // requirements: understanding of how to format the cropped image URLs

        public ActionResult Index(string id)
        {
            return View();
        }

        public PartialViewResult Grid(string term = null, bool includeAll = false, bool includeLinks = true, int max = 0, int patchSize = 100, string title = "")
        {
            ViewBag.IncludeLinks = includeLinks;
            ViewBag.PatchSize = patchSize;
            ViewBag.Title = title;

            var labels = _database.GetCollection<Label>("labels");

            List<Label> cells = new List<Label>();
            var query = Query.Null;
            if (!string.IsNullOrEmpty(term))
                query = Query.Matches("Tag", new BsonRegularExpression(".*" + term + ".*"));

            if (includeAll)
            {
                cells = labels.Find(query).ToList();
                if (max > 0)
                    cells = cells.Take(max).ToList();
            }
            else
            {
                var examples = labels.Group(query, "Tag", new BsonDocument("label", null), new BsonJavaScript("function(doc, out){out.label = doc;}"), null);
                cells = examples.Select(o => BsonSerializer.Deserialize<Label>(o["label"].AsBsonDocument)).ToList();
            }

            return PartialView("_TagGrid", cells);
        }

        public ActionResult Examples(string id)
        {
            ViewBag.Tag = id;

            return View();
        }

        public JsonResult List(string term)
        {
            var labels = _database.GetCollection<Label>("labels");

            term = term.ToLower();

            var tags = labels.Distinct<string>("Tag");
            return Json(tags.Where(o => o.Contains(term)).Select(o => new { id = o, label = o, value = o }), JsonRequestBehavior.AllowGet);
        }
    }
}
