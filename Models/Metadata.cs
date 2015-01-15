using AlmaDUploader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlmaDUploader.Models
{
    public class Metadata
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string Publisher { get; set; }
        public DateTime? PublicationDate { get; set; }
        public string Notes { get; set; }

        public string ToMetadataFormat(string format)
        {
            if (Utilities.AllPropertiesNull(this, new string[] { "Serialized" }))
                return null;

            if (!File.Exists(".\\Data\\" + format + ".xslt"))
                return null;

            return Utilities.TransformXml(this, ".\\Data\\" + format + ".xslt");
        }

        [Newtonsoft.Json.JsonIgnore]
        public string Serialized
        {
            get 
            {
                 if (Utilities.AllPropertiesNull(this, new string[] {"Serialized"}))
                    return null;
                else
                    return Newtonsoft.Json.JsonConvert.SerializeObject(this); 
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                var jData = Newtonsoft.Json.JsonConvert.DeserializeObject<Metadata>(value);
                this.Title = jData.Title;
                this.Author = jData.Author;
                this.ISBN = jData.ISBN;
                this.Publisher = jData.Publisher;
                this.PublicationDate = jData.PublicationDate;
                this.Notes = jData.Notes;
            }
        }
    }
}
