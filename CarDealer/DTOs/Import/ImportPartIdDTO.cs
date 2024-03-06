using System.Xml.Serialization;

namespace CarDealer.DTOs.Import
{
    [XmlType("PartId")]
    public class ImportPartIdDTO
    {
        [XmlAttribute("id")]
        public int Id { get; set; }
    }
}