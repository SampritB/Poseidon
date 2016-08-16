using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using System.Configuration;
using Poseidon.Models;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Drawing;
using ImageResizer;
using System.IO;
using System.Drawing.Imaging;
using System.Xml.Linq;


namespace Poseidon.Controllers
{
    [Authorize]
    public class ImageController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        private Brush _userBrush;
        private Brush _otherBrush;

        public ImageController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);

            _userBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            _otherBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 255));
        }
        //
        // GET: /Image/

        public FileResult Index(string id, bool drawLabels = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return File(new byte[] { }, "image/jpeg", "404.jpg");
            }
            var images = _database.GetCollection<ImageInfo>("images");
            var labels = _database.GetCollection<Label>("labels");

            ImageInfo image = images.FindOne(Query.EQ("_id", new ObjectId(id)));
            var imageLabels = labels.Find(Query.EQ("ImageInfoId", new ObjectId(id)));

            var file = _database.GridFS.FindOne(Query.EQ("_id", new ObjectId(image.FileId)));
            string fileName = file.Name;
            using (var stream = file.OpenRead())
            {
                using (Bitmap bmp = new Bitmap(stream))
                {
                    if (drawLabels)
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            foreach (var label in imageLabels)
                            {
                                Brush fill;
                                using (Pen stroke = new Pen(Color.Black, 4))
                                {
                                    RectangleF bbox;
                                    if (label.IsEllipse)
                                    {
                                        fill = label.UserName.Equals(User.Identity.Name) ? _userBrush : _otherBrush;
                                        bbox = new RectangleF(label.X - label.XRadius, label.Y - label.YRadius, label.XRadius * 2, label.YRadius * 2);
                                    }
                                    else
                                    {
                                        fill = label.UserName.Equals(User.Identity.Name) ? Brushes.White : Brushes.Blue;
                                        bbox = new RectangleF(label.X - 30, label.Y - 30, 60, 60);
                                    }
                                    g.FillEllipse(fill, bbox);
                                    g.DrawEllipse(stroke, bbox);
                                }
                            }
                        }
                    }
                    using (MemoryStream imageBuff = new MemoryStream())
                    {
                        if (Request.QueryString.Count == 0)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(imageBuff);
                        }
                        else
                        {
                            ResizeSettings settings = new ResizeSettings(Request.QueryString);
                            settings.CropTopLeft = fix(settings.CropTopLeft, bmp.Width, bmp.Height);
                            settings.CropBottomRight = fix(settings.CropBottomRight, bmp.Width, bmp.Height);
                            settings.Format = "jpg";
                            ImageBuilder.Current.Build(bmp, imageBuff, settings);
                        }
                        return File(imageBuff.GetBuffer(), "image/jpeg", fileName);
                    }
                }
            }
        }

        private PointF fix(PointF point, float width, float height)
        {
            if (point.X < 0)
                point.X = 0;
            if (point.X > width)
                point.X = width;
            if (point.Y < 0)
                point.Y = 0;
            if (point.Y > height)
                point.Y = height;
            return point;
        }

        public ActionResult Info(string id)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            ImageInfo info = images.FindOne(Query.EQ("_id", new ObjectId(id)));

            return View(info);
        }

        [HttpPost]
        public ActionResult Info(ImageInfo info)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            images.Save(info);

            return View(info);
        }

        [HttpPost]
        public ActionResult Collection(HttpPostedFileBase file, string folder)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            var folderItems = _database.GetCollection<FolderItem>("folderItems");

            Bitmap bmp = (Bitmap)Bitmap.FromStream(file.InputStream);
            ImageInfo image = new ImageInfo();
            image.CreateDate = DateTime.UtcNow;
            image.UserID = User.Identity.Name;
            image.Width = bmp.Width;
            image.Height = bmp.Height;
            image.Title = file.FileName;
            images.Insert(image);

            using (MemoryStream imageBuff = new MemoryStream())
            {
                bmp.Save(imageBuff, ImageFormat.Jpeg);
                imageBuff.Seek(0, SeekOrigin.Begin);
                var gridFSInfo = _database.GridFS.Upload(imageBuff, image.Id + ".jpg");
                image.FileId = gridFSInfo.Id.ToString();
            }

            images.Save(image);

            if (!string.IsNullOrEmpty(folder))
            {
                FolderItem fi = new FolderItem
                {
                    FolderId = folder,
                    ImageInfoId = image.Id
                };
                folderItems.Insert(fi);
            }

            return View(images.FindAll());
        }

        public ActionResult Collection()
        {
            var images = _database.GetCollection<ImageInfo>("images");

            return View(images.FindAll());
        }

        public PartialViewResult Tags(string id, bool editable = true)
        {
            ViewBag.IsEditable = editable;
            var imageTags = _database.GetCollection<ImageTag>("imageTags");

            List<ImageTag> tags = imageTags.Find(Query.EQ("ImageInfoId", new ObjectId(id))).ToList();

            ViewBag.ImageInfoId = id;

            return PartialView("_Tags", tags);
        }

        public PartialViewResult AddTag(string id, string key, string value)
        {
            var imageTags = _database.GetCollection<ImageTag>("imageTags");
			System.Diagnostics.Debug.WriteLine("id: " + id + " key: " + key + " value: " + value);
			if (value == "deleteit")
			{
				var imagess = _database.GetCollection<ImageInfo>("images");

				imagess.Remove(Query.EQ("_id", new ObjectId(id)));

				var imagesss = _database.GetCollection<Label>("labels");

				imagess.Remove(Query.EQ("ImageInfoId", new ObjectId(id)));

				string lastchar = id.Substring((id.Length - 1), 1);
				System.Diagnostics.Debug.WriteLine(lastchar);
				try
				{
					int addone = Int32.Parse(lastchar);

					addone = addone + 1;
					lastchar = addone.ToString();
					id = id.Remove(id.Length - 1);
					id = id + addone;
				}
				catch(System.FormatException e)
				{
					System.Diagnostics.Debug.WriteLine(lastchar);
					char mylastchar = lastchar[0];
					mylastchar++;
					id = id.Remove(id.Length - 1);
					id = id + mylastchar;

				}
				var images = _database.GetCollection<ImageInfo>("fs.files");

				images.Remove(Query.EQ("_id", new ObjectId(id)));
			}

			ImageTag tag = new ImageTag { ImageInfoId = id, Key = key, Value = value };
            imageTags.Insert(tag);

            return Tags(id);
        }

        public PartialViewResult RemoveTag(string id, string imageInfoId)
        {
            var imageTags = _database.GetCollection<ImageTag>("imageTags");

            imageTags.Remove(Query.EQ("_id", new ObjectId(id)));

			return Tags(imageInfoId);
        }

		
	}
}
