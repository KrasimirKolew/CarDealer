
using System.Xml.Serialization;

namespace CarDealer.DTOs.Import
{
    [XmlType("Part")]
    public class ImportPartsDTO
    {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("price")]
        public double Price { get; set; }
        [XmlElement("quantity")]
        public int Quantity { get; set; }
        [XmlElement("supplierId")]
        public int SupplierId { get; set; }
    }
}
