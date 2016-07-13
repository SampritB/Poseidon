using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Poseidon.Models
{
    public class Label
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ImageInfoId { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float XRadius { get; set; }
        public float YRadius { get; set; }

        public bool IsEllipse { get; set; }

        public float TranslateX { get; set; }
        public float TranslateY { get; set; }
        public int Scale { get; set; }

        public string Tag { get; set; }

        public string UserName { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        public string Comment { get; set; }

        public int Confidence { get; set; }

        public string Crop
        {
            get
            {
                float scale = Scale * .1f;
                float x1, x2, y1, y2;
                if (XRadius > 0)
                {
                    x1 = X - XRadius;
                    y1 = Y - YRadius;
                    x2 = X + XRadius;
                    y2 = Y + YRadius;
                }
                else
                {
                    x1 = X - 50;
                    y1 = Y - 50;
                    x2 = X + 50;
                    y2 = Y + 50;
                }
                return string.Join(",", (int)x1, (int)y1, (int)x2, (int)y2);
            }
        }
    }
}