using SQLite;

namespace PublicTransportManager
{
    public class Lines
    {
        [PrimaryKey]
        public string Line_Id { get; set; }
        public string Description { get; set; }
    }

    public class Stations
    {
        [PrimaryKey]
        public int Station_Id { get; set; }
        public string Sign { get; set; }
        public string Name { get; set; }
        public double CoordinatesX { get; set; }
        public double CoordinatesY { get; set; }
    }

    public class LinesStations
    {
        [Indexed]
        public string Line_Id { get; set; }
        [Indexed]
        public int Station_Id { get; set; }
    }

}