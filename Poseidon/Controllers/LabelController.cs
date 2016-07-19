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
using System.IO;

namespace Poseidon.Controllers
{
    [Authorize]
    public class LabelController : Controller
    {
        private const int VIEWPORT_WIDTH = 1600;
        private const int VIEWPORT_HEIGHT = 900;

        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public LabelController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);
        }
        //
        // GET: /Label/

        public ActionResult Index(string id)
        {
            var images = _database.GetCollection<ImageInfo>("images");

            ViewBag.Images = images.FindAll();

            var labels = _database.GetCollection<Label>("labels");

            LabelModel model = new LabelModel
            {
                Scale = 5,
                TranslateX = 0,
                TranslateY = 0,
            };

           

            if (!string.IsNullOrEmpty(id))
            {
                model.ImageInfo = images.FindOne(Query.EQ("_id", new ObjectId(id)));
                model.Labels = labels.Find(Query.EQ("ImageInfoId", new ObjectId(id))).OrderBy(o => o.UpdateDate).ToList();

                double num,den;
                if (model.ImageInfo.Width > model.ImageInfo.Height)
                {
                    num = VIEWPORT_WIDTH;
                    den = model.ImageInfo.Width;
                }
                else
                {
                    num = VIEWPORT_HEIGHT;
                    den = model.ImageInfo.Height;
                }
                int scale = (int)(10*num / den);
                scale = Math.Max(scale, 1);
                scale = Math.Min(scale, 20);
                model.Scale = scale;
            }
            else model.Labels = new List<Label>();

            return View(model);
        }

        public PartialViewResult Image(LabelModel model)
        {        
            return PartialView("_Image", model);
        }

        public JsonResult Create(float x, float y, float xRadius, float yRadius, bool isEllipse, float translateX, float translateY, float scale, string imageInfoId, string tag)
        {
            tag = tag.ToLower();
            Label label = new Label
            {
                X = x,
                Y = y,
                XRadius = xRadius,
                YRadius = yRadius,
                IsEllipse = isEllipse,
                TranslateX = translateX,
                TranslateY = translateY,
                ImageInfoId = imageInfoId,
                Scale = (int)scale,
                Tag = tag,
                UserName = User.Identity.Name,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            };
            var labels = _database.GetCollection<Label>("labels");
            labels.Insert(label);

            return Json(new { id = label.Id.ToString() });
        }

        public JsonResult Update(string id, float x, float y, float xRadius, float yRadius, bool isEllipse, float translateX, float translateY, float scale, string tag)
        {
            var labels = _database.GetCollection<Label>("labels");
            var images = _database.GetCollection<ImageInfo>("images");

            Label label = labels.FindOne(Query.EQ("_id", new ObjectId(id)));
            ImageInfo info = images.FindOne(Query.EQ("_id", new ObjectId(label.ImageInfoId)));

            tag = tag.ToLower();

            if (x < 0 || x >= info.Width || y < 0 || y >= info.Height)
            {
                labels.Remove(Query.EQ("_id", new ObjectId(id)));
            }
            else
            {
                label.X = x;
                label.Y = y;
                label.XRadius = xRadius;
                label.YRadius = yRadius;
                label.IsEllipse = isEllipse;
                label.TranslateX = translateX;
                label.TranslateY = translateY;
                label.Scale = (int)scale;
                label.Tag = tag;
                label.UpdateDate = DateTime.UtcNow;
                label.UserName = User.Identity.Name;
                labels.Save(label);
            }

            return Json(new { id = id, result = "success" });
        }

        public JsonResult Delete(string id)
        {
            var labels = _database.GetCollection<Label>("labels");

            labels.Remove(Query.EQ("_id", new ObjectId(id)));

            return Json(new { id = id, result = "success" });
        }

        public JsonResult Tag(string id, string tag)
        {
            if (tag == ""||tag=="\\"||tag=="/"||tag=="@")
            {
                tag = "untagged";
            }
			

			//tag = replacer(tag, "\\", " ");


			var labels = _database.GetCollection<Label>("labels");

            tag = tag.ToLower();

            Label label = labels.FindOne(Query.EQ("_id", new ObjectId(id)));
            if (label != null)
            {
                label.Tag = tag;
                label.UpdateDate = DateTime.UtcNow;
                label.UserName = User.Identity.Name;
                labels.Save(label);
                return Json(new { result = "success" });
            }
            else return Json(new { result = "invalid id" });            
        }

		//method to replace special characters

		public String replacer(string tag, string bad, String replace)
		{
			tag.Replace(bad, replace);

			return tag;
		}

        public PartialViewResult Info(string id)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            var labels = _database.GetCollection<Label>("labels");

            Label label = labels.FindOne(Query.EQ("_id", new ObjectId(id)));
            ImageInfo image = images.FindOne(Query.EQ("_id", new ObjectId(label.ImageInfoId)));

            ViewBag.Title = image.Title;

            return PartialView("_Dialog", label);
        }

        public ActionResult UpdateInfo(string id, string tag, string comment)
        {
            var labels = _database.GetCollection<Label>("labels");

            tag = tag.ToLower();

            Label label = labels.FindOne(Query.EQ("_id", new ObjectId(id)));
            label.Tag = tag;
            label.UpdateDate = DateTime.UtcNow;
            label.UserName = User.Identity.Name;
            label.Comment = comment;
            labels.Save(label);

            return RedirectToAction("Grid", "Tag", new { term = tag, includeAll = true, includeLinks = false, patchSize = 50, max = 16, title = "Samples for " + tag });
        }
    }
}
