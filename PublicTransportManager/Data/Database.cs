using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using SQLite;
using Android.Gms.Maps.Model;

namespace PublicTransportManager
{
    public class Database : SQLiteConnection
    {
        public Database(string databasePath) : base(databasePath)
        {
        }

        public List<Stations> AllSationsFormLine(string lineId)
        {
            return Query<Stations>("select * " +
                                   "from Stations as s " +
                                   "join LinesStations as ls " +
                                   "on ls.Station_Id = s.Station_Id " +
                                   "where ls.Line_Id = ?",
                                   lineId);
        }
        public List<string> LinesIDFromStationsID(int station_ID)
        {
            var linesIdList = new List<string>();
            var q = Query<LinesStations>("select ls.Line_Id " +
                                         "from LinesStations as ls " +
                                         "join Stations as s " +
                                         "on s.Station_Id = ls.Station_Id " +
                                         "where s.Station_Id = ?",
                                         station_ID);
            foreach (var l in q)
            {
                linesIdList.Add(l.Line_Id);
            }

            return linesIdList;
        }
        public List<string> AllLinesID()
        {
            var listLines = new List<string>();
            var q = Query<Lines>("select l.Line_Id from Lines as l");

            foreach (var l in q)
            {
                listLines.Add(l.Line_Id);
            }
            return listLines;
        }
        public int GetStationID(double coordinatesX, double coordinatesY)
        {
            var q = Table<Stations>().Where(s => s.CoordinatesX == coordinatesX && s.CoordinatesY == coordinatesY).FirstOrDefault();
            return q.Station_Id;
        }

        public Stations GetStation(int station_id)
        {
            return Table<Stations>().Where(s => s.Station_Id == station_id).FirstOrDefault();
        }

        public List<LatLng> AllStationsLocations()
        {
            var q = Query<Stations>("select s.CoordinatesX, s.CoordinatesY " +
                                  "from Stations as s");
            var latlngList = new List<LatLng>();

            foreach (var s in q)
            {
                latlngList.Add(new LatLng(s.CoordinatesX, s.CoordinatesY));
            }
            return latlngList;
        }

        public List<LatLng> GetAllStationsLocationFromLine(string lineID)
        {
            var list = new List<LatLng>();

            var q = Query<Stations>("select s.CoordinatesX, s.CoordinatesY "
                                  + "from Stations as s "
                                  + "inner join LinesStations as ls "
                                  + "on s.Station_Id = ls.Station_Id "
                                  + "where ls.Line_Id = ?"
                                  , lineID);

            foreach (var s in q)
            {
                list.Add(new LatLng(s.CoordinatesX, s.CoordinatesY));
            }
            return list;

        }

        public List<Lines> AllLines()
        {
            return Query<Lines>("select * from Lines");
        }

        public Lines GetLine(string lineId)
        {
            return Table<Lines>().Where(l => l.Line_Id == lineId).FirstOrDefault();
        }

    }


}