using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Poseidon.Models
{
    public class FolderItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FolderId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ImageInfoId { get; set; }
    }
}