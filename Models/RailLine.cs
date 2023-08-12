using NetTopologySuite.Geometries;

namespace WGS_To_ITM_GeoCoding_Service.Models
{
    public class RailLine
    {
        public int OBJECTID { get; set; }
        public Geometry? SHAPE { get; set; }
    }
}
