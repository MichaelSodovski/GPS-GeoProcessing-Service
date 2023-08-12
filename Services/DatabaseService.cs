using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.CoordinateSystems;
using Oracle.ManagedDataAccess.Client;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using WGS_To_ITM_GeoCoding_Service.Models;
using System.Diagnostics;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using System.Data;
using Serilog;

namespace WGS_To_ITM_GeoCoding_Service.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task PerformDataBaseOperations()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (OracleConnection conn = new OracleConnection(_connectionString)) {
                using (OracleCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        conn.Open();
                        cmd.BindByName = true;

                        // pull the last OBJECT_ID of the last batch to use it to get all the points from the next batch:
                        int lastid = 0;
                        try
                        {
                            cmd.Parameters.Clear();
                            cmd.CommandText = "SELECT * FROM OBJECT_ID_STORE";
                            using (OracleDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Log.Information("No rows returned.");
                                }
                                while (reader.Read())
                                {
                                    lastid = reader.GetInt32(0);
                                    Log.Information($"LAST OBJECT_ID of batch = {lastid}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An error occurred: {Message}", ex.Message);
                        }

                        // pull content of GPS_DATA table to convert the points: 
                        List<Models.Point> wgsPoints = new List<Models.Point>();
                        cmd.CommandText = "SELECT OBJECT_ID, X, Y FROM GPS_DATA WHERE OBJECT_ID > :lastid"; // from lastid and forward.
                        cmd.Parameters.Clear();  // Clear parameters
                        cmd.BindByName = true; // Ensure parameters are bound by name
                        cmd.Parameters.Add("lastid", OracleDbType.Int32, lastid, ParameterDirection.Input); // Add the parameter
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int OBJECT_ID = reader.GetInt32(0);
                                float X = reader.GetFloat(1);
                                float Y = reader.GetFloat(2);
                                wgsPoints.Add(new Models.Point { OBJECT_ID = OBJECT_ID, X = X, Y = Y });
                                Log.Information($"Point: OBJECT_ID = {OBJECT_ID},X = {X}, Y = {Y}");
                            }
                        }

                        // get the last item from the list and retrieve its OBJECT_ID
                        int lastItem = 0;
                        if (wgsPoints.Any())
                        {
                            lastItem = wgsPoints.Last().OBJECT_ID;
                            Log.Information($"The last item in the array is: {lastItem}");
                        }
                        else
                        {
                            Log.Warning("List is empty.");
                        }

                        // update the GPS_DATA_STORE table with the retrieved last OBJECT_ID.
                        cmd.CommandText = "UPDATE OBJECT_ID_STORE SET GPS_DATA_ID = :lastItem";
                        cmd.Parameters.Clear();  // Clear parameters
                        cmd.BindByName = true; // Ensure parameters are bound by name
                        OracleParameter LastIdParam = new OracleParameter("lastItem", lastItem)
                        {
                            Value = lastItem
                        };
                        cmd.Parameters.Add(LastIdParam);
                        cmd.ExecuteNonQuery();

                        // handle convertion elements:
                        CoordinateSystemFactory csFac = new CoordinateSystemFactory();
                        string wgsWkt = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]]";
                        string itmWkt = "PROJCS[\"Israel_TM_Grid\",GEOGCS[\"GCS_Israel_1993\",DATUM[\"D_Israel_1993\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",31.7343936111111],PARAMETER[\"central_meridian\",35.2045169444444],PARAMETER[\"scale_factor\",1.0000067],PARAMETER[\"false_easting\",219529.584],PARAMETER[\"false_northing\",626907.39],UNIT[\"Meter\",1]]";
                        ICoordinateSystem wgs = csFac.CreateFromWkt(wgsWkt);
                        ICoordinateSystem itm = csFac.CreateFromWkt(itmWkt);
                        CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
                        ICoordinateTransformation trans = ctfac.CreateFromCoordinateSystems(wgs, itm);

                        // Convert the WGS points to ITM points:
                        List<Models.Point> itmPoints = new List<Models.Point>();
                        foreach (var wgsPoint in wgsPoints)
                        {
                            double[] point = new double[] { (double)wgsPoint.X, (double)wgsPoint.Y };
                            double[] itmPoint = trans.MathTransform.Transform(point);
                            //Log.Information($"ITM Point: X = {itmPoint[0]} Y = {itmPoint[1]}" );
                            Models.Point newITMpoint = new Models.Point { OBJECT_ID = wgsPoint.OBJECT_ID, X = (float)itmPoint[0], Y = (float)itmPoint[1] };
                            itmPoints.Add(newITMpoint);
                            Log.Information($"Converted Point: OBJECT_ID = {newITMpoint.OBJECT_ID}, X = {newITMpoint.X:F11}, Y = {newITMpoint.Y:F11}");
                        }

                        // pull out all the data from the rail lines's table SHAPE column and OBJECTID column. 
                        List<RailLine> RailLineShapes = new List<RailLine>();
                        cmd.CommandText = "SELECT OBJECTID, SDE.ST_AsText(SHAPE) AS RAIL_LINE_SHAPE FROM SDE.RAIL_LINES";
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            WKTReader WKTreader = new WKTReader();
                            while (reader.Read())
                            {
                                int OBJECTID = (int)reader.GetInt16(0);
                                string wktFromDatabase = reader.GetString(reader.GetOrdinal("RAIL_LINE_SHAPE"));
                                Geometry shapeGeometry = WKTreader.Read(wktFromDatabase);
                                RailLine railLineEntity = new RailLine { OBJECTID = OBJECTID, SHAPE = shapeGeometry };
                                RailLineShapes.Add(railLineEntity);
                                //Log.Information($"--------------------------------------------------------------------");
                                //Log.Information($"OBJECTID: {railLineEntity.OBJECTID}, SHAPE = {railLineEntity.SHAPE}");
                            }
                        }
                        //Log.Information($"Total number of shapes retrieved: {RailLineShapes.Count}");

                        int counter = 0;
                        foreach (var itmPoint in itmPoints)
                        {
                            // Create shape point from each point in the itmPoints array:
                            NetTopologySuite.Geometries.Point point = new NetTopologySuite.Geometries.Point((double)itmPoint.X, (double)itmPoint.Y);

                            foreach (RailLine railLineShape in RailLineShapes)
                            {
                                double distance = point.Distance(railLineShape.SHAPE);
                                // If the distance is less than or equal to 3 meters, store the rail line ID for that point
                                if (distance <= 3.0)
                                {
                                    counter++;
                                    Log.Information($"Point: {point}, OBJECT_ID: {itmPoint.OBJECT_ID} is within 3 meters of Rail Line: {railLineShape.OBJECTID}");
                                    cmd.Parameters.Clear();
                                    cmd.CommandText = "INSERT INTO RAIL_LINE_DATA (OBJECTID, RAIL_LINE_ID, TIMESTAMP) VALUES (:OBJECT_ID, :RAIL_LINE_ID, :TIMESTAMP)";
                                    OracleParameter OBJECT_ID = new OracleParameter("OBJECT_ID", itmPoint.OBJECT_ID);
                                    OracleParameter RAIL_LINE_ID = new OracleParameter("RAIL_LINE_ID", railLineShape.OBJECTID);
                                    OracleParameter TIMESTAMP = new OracleParameter("TIMESTAMP", OracleDbType.TimeStamp)
                                    {
                                        Value = DateTime.Now
                                    };
                                    cmd.Parameters.Add(OBJECT_ID);
                                    cmd.Parameters.Add(RAIL_LINE_ID);
                                    cmd.Parameters.Add(TIMESTAMP);
                                    cmd.ExecuteNonQuery();
                                } 
                            }
                        }

                        Log.Information($"Number of points within 3 meters of a rail line: {counter}");
                        stopwatch.Stop();
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        Log.Information($"Total operation time: {stopwatch.Elapsed.TotalSeconds} seconds");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
            }
        }
    }
}
