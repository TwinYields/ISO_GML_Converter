
/*
 *  This file is part of ISO_GML_Converter
 *
 *  Copyright 2022 Juha Backman / Natural Resources Institute Finland
 *
 *  ISO_GML_Converter is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as
 *  published by the Free Software Foundation, either version 3 of
 *  the License, or (at your option) any later version.
 *
 *  ISO_GML_Converter is distributed in the hope that it will be useful, but
 *  WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with ISO_GML_Converter.
 *  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ISO_GML_Converter
{
    public class DPD_Reference
    {
        public string element;
        public string DDI;

        public DPD_Reference(string ref_element = "", string ref_DDI = "")
        {
            element = ref_element;
            DDI = ref_DDI;
        }

        public DPD_Reference(DPD_Reference DPD)
        {
            element = DPD.element;
            DDI = DPD.DDI;
        }
    }

    public class LogElement
    {
        public string name;
        public Type type;
        public DPD_Reference DPD;

        public virtual string getValueString(int index)
        {
            return "";
        }

        public virtual int getSize()
        {
            return 0;
        }
    }

    public class LogElementType<T> : LogElement
    {
        public List<T> values;

        public LogElementType(string description)
        {
            name = description;
            values = new List<T>();
            type = typeof(T);
        }

        public override string getValueString(int index)
        {
            return values.ElementAt(index).ToString();
        }

        public override int getSize()
        {
            return values.Count;
        }
    }

    public class Point
    {
        public double x;
        public double y;
        public double z;

        public DPD_Reference DPD_x = null;
        public DPD_Reference DPD_y = null;
        public DPD_Reference DPD_z = null;

        public Point(double point_x = 0, double point_y = 0, double point_z = 0)
        {
            x = point_x;
            y = point_y;
            z = point_z;
        }

        public Point(Point p)
        {
            x = p.x;
            y = p.y;
            z = p.z;
            DPD_x = p.DPD_x;
            DPD_y = p.DPD_y;
            DPD_z = p.DPD_z;
        }

        public double DotProduct(Point p)
        {
            return x * p.x + y * p.y + z * p.z;
        }

        public Point RotateZ(double angle)
        {
            return new Point(Math.Cos(angle) * x - Math.Sin(angle) * y, Math.Sin(angle) * x + Math.Cos(angle) * y);
        }

        public static Point operator  + (Point p1, Point p2)
        {
            return new Point(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
        }

        public static Point operator - (Point p1, Point p2)
        {
            return new Point(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
        }

        public override string ToString()
        {
            string output = "";

            output += "x: ";
            if (DPD_x != null)
                output += "<" + DPD_x.element + " (" + DPD_x.DDI + ")>";
            else
                output += x;

            output += " y: ";
            if (DPD_y != null)
                output += "<" + DPD_y.element + " (" + DPD_y.DDI + ")>";
            else
                output += y;

            output += " z: ";
            if (DPD_z != null)
                output += "<" + DPD_z.element + " (" + DPD_z.DDI + ")>";
            else
                output += z;

            return output;
        }
    }

    public class TractorImplementGeometry
    {
        public enum ConnectionType
        {
            Mounted,
            Towed
        }
        public ConnectionType connection;

        public string element;      // reference to DeviceElementReferencePoint
        public string description;

        public Point TractorDRP;    // DeficeReferencePoint
        public Point TractorNRP;    // NavigationReferencePoint
        public Point TractorCRP;    // ConnectorReferencePoint

        public Point ImplementDRP;  // DeficeReferencePoint
        public Point ImplementCRP;  // ConnectorReferencePoint
        public Point ImplementERP;  // DeviceElementReferencePoint

        public DPD_Reference yaw;                  // tractor yaw angle
        public DPD_Reference connectionangle;      // angle between the tractor and the implement


        public List<LogElement> datalogheader = new List<LogElement>();
        public List<LogElement> datalogdata = new List<LogElement>();


        public Point getValues(Point point, int index)
        {
            if (point.DPD_x != null)
            {
                var DPD = datalogdata.Where(data => data.DPD.DDI == point.DPD_x.DDI && data.DPD.element == point.DPD_x.element);
                if(DPD.Count() == 1)
                {
                    point.x = (double)((LogElementType<System.Int32>)DPD.Single()).values[index] * 0.001;
                }
                else
                {
                    // TODO: this should throw exception
                }
            }

            if (point.DPD_y != null)
            {
                var DPD = datalogdata.Where(data => data.DPD.DDI == point.DPD_y.DDI && data.DPD.element == point.DPD_y.element);
                if (DPD.Count() == 1)
                {
                    point.y = -(double)((LogElementType<System.Int32>)DPD.Single()).values[index] * 0.001;
                }
                else
                {
                    // TODO: this should throw exception
                }
            }

            if (point.DPD_z != null)
            {
                var DPD = datalogdata.Where(data => data.DPD.DDI == point.DPD_z.DDI && data.DPD.element == point.DPD_z.element);
                if (DPD.Count() == 1)
                {
                    point.z = -(double)((LogElementType<System.Int32>)DPD.Single()).values[index] * 0.001;
                }
                else
                {
                    // TODO: this should throw exception
                }
            }

            return point;
        }
    }

    public class TimeLogData
    {
        public string taskname;
        public string field;
        public string farm;
        public List<string> devices;
        public List<TractorImplementGeometry> geometry = new List<TractorImplementGeometry>();

        public List<LogElement> datalogheader = new List<LogElement>();
        public List<LogElement> datalogdata = new List<LogElement>();
    }

    class Program
    {
        public TimeLogData TLGdata = new TimeLogData();

        enum OutputType
        {
            GML,
            CSV
        };

        OutputType outputType = OutputType.GML;
        bool cartesianCoordinates = false;
        bool useGeometry = true;


        static void Main(string[] args)
        {
            Program program = new Program();

            program.run(args);
        }

        void run(string[] args)
        {
            string taskfilename = Directory.GetCurrentDirectory() + "\\TASKDATA.XML";

            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    string[] options = arg.Split('=');
                                           
                    switch(options[0])
                    {
                        case "-c":
                        case "-cartesian":
                            cartesianCoordinates = true;
                            break;
                        case "-n":
                        case "-nosimulation":
                            useGeometry = false;
                            break;
                        case "-o":
                        case "-output":
                            switch(options[1])
                            {
                                case "GML":
                                case "gml":
                                    outputType = OutputType.GML;
                                    break;
                                case "CSV":
                                case "csv":
                                    outputType = OutputType.CSV;
                                    break;
                                default:
                                    Console.WriteLine("Unknown output type.");
                                    Console.WriteLine("Possible values are [GML, CSV]");
                                    return;
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown parameter");
                            Console.WriteLine("Possible values are:");
                            Console.WriteLine("-o/-output=[GML, CSV]");
                            return;
                    }
                }
                else
                {
                    taskfilename = args[0];
                }
            }
                            

            Console.WriteLine("Open file " + taskfilename);
            Console.WriteLine("==============================================");
                                   
            if (!convertTaskFile(taskfilename))
            {
                Console.WriteLine("ERROR IN CONVERTING FILE");
                return;
            }
        }

        string generateName(string description)
        {
            string name = TLGdata.farm + " " + TLGdata.field + " " + TLGdata.taskname + " " + description;

            LogElementType<System.String> TimeStartDATE = (LogElementType<System.String>)TLGdata.datalogheader.Where(val => val.name == "TimeStartDATE").Single();
            name += " " + TimeStartDATE.values.First();

            LogElementType<System.String> TimeStartTOFD = (LogElementType<System.String>)TLGdata.datalogheader.Where(val => val.name == "TimeStartTOFD").Single();
            name += " " + TimeStartTOFD.values.First().Replace(":", ".");

            char[] invalids = System.IO.Path.GetInvalidFileNameChars();
            name = String.Join("", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

            return name;
        }

        bool hasGeometryOffset(XElement element)
        {
            var DOR = element.Descendants("DOR").Attributes("A").Select(atr => atr.Value).ToList();

            var DPD_offsetX = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 134);
            if (DPD_offsetX.Any())
                return true;

            var DPD_offsetY = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 135);
            if (DPD_offsetY.Any())
                return true;

            var DPD_offsetZ = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 136);
            if (DPD_offsetZ.Any())
                return true;

            var DPT_offsetX = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 134);
            if (DPT_offsetX.Any())
                return true;

            var DPT_offsetY = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 135);
            if (DPT_offsetY.Any())
                return true;

            var DPT_offsetZ = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 136);
            if (DPT_offsetZ.Any())
                return true;

            return false;
        }


        Point extractGeometryOffset(XElement element)
        {
            Point point = new Point();

            var DOR = element.Descendants("DOR").Attributes("A").Select(atr => atr.Value).ToList();

            var DPD_offsetX = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 134);
            if (DPD_offsetX.Any())
                point.DPD_x = new DPD_Reference(element.Attribute("A").Value, DPD_offsetX.Single().Attribute("B").Value);

            var DPD_offsetY = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 135);
            if (DPD_offsetY.Any())
                point.DPD_y = new DPD_Reference(element.Attribute("A").Value, DPD_offsetY.Single().Attribute("B").Value);

            var DPD_offsetZ = element.Ancestors("DVC").Single().Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && Convert.ToInt32(dpd.Attribute("B").Value, 16) == 136);
            if (DPD_offsetZ.Any())
                point.DPD_z = new DPD_Reference(element.Attribute("A").Value, DPD_offsetZ.Single().Attribute("B").Value);

            var DPT_offsetX = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 134);
            if (DPT_offsetX.Any())
                point.x = (double)Int32.Parse(DPT_offsetX.Single().Attribute("C").Value) * 0.001;
            
            var DPT_offsetY = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 135);
            if (DPT_offsetY.Any())
                point.y = -(double)Int32.Parse(DPT_offsetY.Single().Attribute("C").Value) * 0.001;

            var DPT_offsetZ = element.Ancestors("DVC").Single().Descendants("DPT").Where(dpt => DOR.Contains(dpt.Attribute("A").Value) && Convert.ToInt32(dpt.Attribute("B").Value, 16) == 136);
            if (DPT_offsetZ.Any())
                point.z = -(double)Int32.Parse(DPT_offsetZ.Single().Attribute("C").Value) * 0.001;

            // Note: ISO 11783-10 defines coordinates:
            //  positive x: driving direction
            //  positive y: to the right relative to the driving direction
            //  positive z: down
            // In here we are using ENU coordinates (x: East, y: Noth, z: Up)

            return point;
        }

        bool extractGeometryInformation(XElement TSK)
        {
            bool retvalue = true;

            // TODO: read True Rotation Point; DDI=306 and 304 (Does anyone use it?)
            // TODO: read Actual relative connection angle; DDI=466  (Does anyone use it?)

            try
            {

                // include devices that are listed in Task and are connected together
                foreach (var CNN in TSK.Elements("CNN"))
                {
                    // TODO: read Connector Type; DDI=157  (0=unknown is default)

                    var DVC_0 = TSK.Ancestors().Descendants("DVC").Where(dvc => dvc.Attribute("A").Value == CNN.Attribute("A").Value).Single();
                    Point CRP_0 = extractGeometryOffset(DVC_0.Descendants("DET").Where(det => det.Attribute("A").Value == CNN.Attribute("B").Value).Single());

                    var DVC_1 = TSK.Ancestors().Descendants("DVC").Where(dvc => dvc.Attribute("A").Value == CNN.Attribute("C").Value).Single();
                    Point CRP_1 = extractGeometryOffset(DVC_1.Descendants("DET").Where(det => det.Attribute("A").Value == CNN.Attribute("D").Value).Single());

                    var NRP = DVC_0.Descendants("DET").Where(det => det.Attribute("C").Value == "7");
                    if (!NRP.Any())
                    {
                        // assumption: The tractor has GNSS mounted on; DVC_0=tractor, DVC_1=implement. Swap if GNSS is not found from DVC_0
                        var DVC_temp = DVC_0;
                        DVC_0 = DVC_1;
                        DVC_1 = DVC_temp;

                        var CRP_temp = CRP_0;
                        CRP_0 = CRP_1;
                        CRP_1 = CRP_temp;
                    }

                    NRP = DVC_0.Descendants("DET").Where(det => det.Attribute("C").Value == "7");
                    if (!NRP.Any())
                        continue;   // No GNSS position found, cannot do anything

                    Point NRP_0 = extractGeometryOffset(NRP.Single());


                    DPD_Reference YAW_reference = null;
                    var YAW_DPD = DVC_0.Descendants("DPD").Where(dpd => Convert.ToInt32(dpd.Attribute("B").Value, 16) == 144);
                    if(YAW_DPD.Any())
                    {
                        var DET = DVC_0.Descendants("DOR").Where(dor => dor.Attribute("A").Value == YAW_DPD.Single().Attribute("A").Value).First().Parent;
                        YAW_reference = new DPD_Reference(DET.Attribute("A").Value, YAW_DPD.Single().Attribute("B").Value);
                    }


                    var DRP_1 = DVC_1.Descendants("DET").Where(det => det.Attribute("F").Value == "0").Single();
                    TLGdata.geometry.Add(new TractorImplementGeometry()
                    {
                        connection = TractorImplementGeometry.ConnectionType.Towed,
                        TractorCRP = CRP_0,
                        TractorNRP = NRP_0,
                        ImplementCRP = CRP_1,
                        ImplementERP = new Point(),
                        yaw = YAW_reference,
                        element = DRP_1.Attribute("A").Value                       
                    });

                    foreach (var ERP in DVC_1.Descendants("DET").Where(det => hasGeometryOffset(det)))
                    {
                        TLGdata.geometry.Add(new TractorImplementGeometry()
                        {
                            connection = TractorImplementGeometry.ConnectionType.Towed,
                            TractorCRP = CRP_0,
                            TractorNRP = NRP_0,
                            ImplementCRP = CRP_1,
                            ImplementERP = extractGeometryOffset(ERP),
                            yaw = YAW_reference,
                            element = ERP.Attribute("A").Value
                        });
                    }
                }

                // include also all other devices that has GNSS mounted on
                foreach (var NRP in TSK.Ancestors().Descendants("DET").Where(det => det.Attribute("C").Value == "7"))
                {
                    var DVC_0 = NRP.Ancestors("DVC");
                    var DRP_0 = DVC_0.Descendants("DET").Where(det => det.Attribute("F").Value == "0").Single();

                    if (!TLGdata.geometry.Where(geometry => geometry.element == DRP_0.Attribute("A").Value).Any())
                    {
                        Point NRP_0 = extractGeometryOffset(NRP);

                        DPD_Reference YAW_reference = null;
                        var YAW_DPD = DVC_0.Descendants("DPD").Where(dpd => Convert.ToInt32(dpd.Attribute("B").Value, 16) == 144);
                        if (YAW_DPD.Any())
                        {
                            var DET = DVC_0.Descendants("DOR").Where(dor => dor.Attribute("A").Value == YAW_DPD.Single().Attribute("A").Value).First().Parent;
                            YAW_reference = new DPD_Reference(DET.Attribute("A").Value, YAW_DPD.Single().Attribute("B").Value);
                        }

                        TLGdata.geometry.Add(new TractorImplementGeometry()
                        {
                            connection = TractorImplementGeometry.ConnectionType.Mounted,
                            TractorCRP = new Point(),
                            TractorNRP = NRP_0,
                            ImplementCRP = new Point(),
                            ImplementERP = new Point(),
                            yaw = YAW_reference,
                            element = DRP_0.Attribute("A").Value
                        });

                        foreach (var ERP in DVC_0.Descendants("DET").Where(det => hasGeometryOffset(det)))
                        {
                            TLGdata.geometry.Add(new TractorImplementGeometry()
                            {
                                connection = TractorImplementGeometry.ConnectionType.Mounted,
                                TractorCRP = new Point(),
                                TractorNRP = NRP_0,
                                ImplementCRP = new Point(),
                                ImplementERP = extractGeometryOffset(ERP),
                                yaw = YAW_reference,
                                element = ERP.Attribute("A").Value
                            });
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("ERROR IN EXTRACTING GEOMETRY INFORMATION");
                Console.WriteLine(ex.ToString());

                retvalue = false;
            }

            TLGdata.geometry.Add(new TractorImplementGeometry()
            {
                connection = TractorImplementGeometry.ConnectionType.Mounted,
                TractorCRP = new Point(),
                TractorNRP = new Point(),
                ImplementCRP = new Point(),
                ImplementERP = new Point(),
                element = "original"
            });

            Console.WriteLine("******* Geometry: **********");
            foreach (var geometry in TLGdata.geometry)
            {
                geometry.description = geometry.element;
                var DET = TSK.Ancestors().Descendants("DET").Where(det => det.Attribute("A").Value == geometry.element);
                if (DET.Any() && DET.Single().Attributes("D").Any())
                {
                    var DVC = DET.Ancestors("DVC").Single();
                    if (DVC.Attributes("B").Any())
                        geometry.description = DVC.Attribute("B").Value;
                    else
                        geometry.description = DVC.Attribute("A").Value;

                    geometry.description += "/ " + DET.Single().Attribute("D").Value;
                }

                Console.WriteLine("Geometry: " + geometry.description);
                Console.WriteLine("DeviceElement: " + geometry.element);
                Console.WriteLine("Connection type: " + geometry.connection);
                Console.WriteLine("TractorCRP: " + geometry.TractorCRP);
                Console.WriteLine("TractorNRP: " + geometry.TractorNRP);
                Console.WriteLine("ImplementCRP: " + geometry.ImplementCRP);
                Console.WriteLine("ImplementERP: " + geometry.ImplementERP);

                if(geometry.yaw != null)
                    Console.WriteLine("Yaw angle: <" + geometry.yaw.element + " (" + geometry.yaw.DDI + ")>");

                Console.WriteLine("----------------------------------------------");
            }

            return retvalue;
        }

        bool extractTimelog(XElement TLG, string directory)
        {
            // read header
            string header_file = directory + TLG.Attribute("A").Value + ".xml";

            XDocument TLGFile;
            try
            {
                TLGFile = XDocument.Load(header_file);
            }
            catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            if (TLGFile.Element("TIM").Attribute("A").Value == "")
            {
                TLGdata.datalogheader.Add(new LogElementType<System.String>("TimeStartTOFD"));
                TLGdata.datalogheader.Add(new LogElementType<System.String>("TimeStartDATE"));
            }
            // Attribute B and C are not valid for TIM in TLG

            foreach (var PTN in TLGFile.Element("TIM").Descendants("PTN"))
            {
                if (PTN.Attribute("A") != null && PTN.Attribute("A").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionNorth"));
                if (PTN.Attribute("B") != null && PTN.Attribute("B").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionEast"));
                if (PTN.Attribute("C") != null && PTN.Attribute("C").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionUp"));
                if (PTN.Attribute("D") != null && PTN.Attribute("D").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.Byte>("PositionStatus"));
                if (PTN.Attribute("E") != null && PTN.Attribute("E").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("PDOP"));
                if (PTN.Attribute("F") != null && PTN.Attribute("F").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("HDOP"));
                if (PTN.Attribute("G") != null && PTN.Attribute("G").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.Byte>("NumberOfSatellites"));
                if (PTN.Attribute("H") != null && PTN.Attribute("H").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcTime"));
                if (PTN.Attribute("I") != null && PTN.Attribute("I").Value == "")
                    TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcDate"));
            }


            foreach (var DLV in TLGFile.Element("TIM").Descendants("DLV"))
            {
                string ProcessDataDDI = DLV.Attribute("A").Value;
                string DeviceElementIdRef = DLV.Attribute("C").Value;

                var DET = TLG.Ancestors().Descendants("DET").Where(det => det.Attribute("A").Value == DeviceElementIdRef).Single();

                LogElementType<System.Int32> logelement;

                try
                {
                    var DOR = DET.Descendants("DOR").Attributes("A").Select(atr => atr.Value).ToList();
                    var DPD = DET.Parent.Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && dpd.Attribute("B").Value == ProcessDataDDI).Single();

                    string DVCdesignator = DET.Parent.Attribute("B").Value;  // Note! optional attribute
                    string DETdesignator = DET.Attribute("D").Value; // Note! optional attribute
                    string DPDdesignator = DPD.Attribute("E").Value; // Note! optional attribute --> Use DDI description instead

                    logelement = new LogElementType<System.Int32>(DVCdesignator + "_" + DETdesignator + "_" + DPDdesignator) { DPD = new DPD_Reference(DeviceElementIdRef, ProcessDataDDI) };
                    try
                    {
                        logelement.values.Add(System.Int32.Parse(DLV.Attribute("B").Value));
                    }
                    catch (System.FormatException)
                    {
                        logelement.values.Add(0);
                    }
                    TLGdata.datalogdata.Add(logelement);
                }
                catch (System.InvalidOperationException)
                {
                    Console.WriteLine("Process data description not found!");

                    logelement = new LogElementType<System.Int32>(DeviceElementIdRef + "_" + ProcessDataDDI) { DPD = new DPD_Reference(DeviceElementIdRef, ProcessDataDDI) };
                    try
                    {
                        logelement.values.Add(System.Int32.Parse(DLV.Attribute("B").Value));
                    }
                    catch (System.FormatException)
                    {
                        logelement.values.Add(0);
                    }
                    TLGdata.datalogdata.Add(logelement);
                }

                // Find the geometry information for current logelement
                var DVC_DET_list = DET.Ancestors("DVC").Descendants("DET");
                var DET_geometry = DET;
                var geometry = TLGdata.geometry.Where(geo => geo.element == DET_geometry.Attribute("A").Value);

                while (!geometry.Any() && DET_geometry.Attribute("B").Value != "0")
                {
                    var parent = DVC_DET_list.Where(det => det.Attribute("B").Value == DET_geometry.Attribute("F").Value);
                    if (parent.Count() == 1)
                        DET_geometry = parent.Single();
                    else
                        break;
                    geometry = TLGdata.geometry.Where(geo => geo.element == DET_geometry.Attribute("A").Value);
                }

                if (geometry.Any() && useGeometry)
                    geometry.Single().datalogdata.Add(logelement);
                else
                    TLGdata.geometry.Where(geo => geo.element == "original").Single().datalogdata.Add(logelement);
            }



            Console.WriteLine(TLG.Attribute("A").Value);
            Console.WriteLine("******* Header: **********");
            foreach (var element in TLGdata.datalogheader)
            {
                Console.WriteLine(element.name);
            }
            Console.WriteLine("******* Data: **********");
            foreach (var geometry in TLGdata.geometry)
            {
                if (geometry.datalogdata.Any())
                {
                    Console.WriteLine("Data measured in geometry position: " + geometry.description);

                    foreach (var element in geometry.datalogdata)
                    {
                        Console.WriteLine(element.name);
                    }
                    Console.WriteLine("----------------------------------------------");
                }
            }
            Console.WriteLine("==============================================");


            // ready binary
            string binary_file = directory + TLG.Attribute("A").Value + ".bin";

            Console.WriteLine("**  Read binary file  **");
            Console.WriteLine(binary_file);

            BinaryReader reader = new BinaryReader(new FileStream(binary_file, FileMode.Open));
            List<LogElement>.Enumerator header = TLGdata.datalogheader.GetEnumerator();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                if (!header.MoveNext())
                {
                    header = TLGdata.datalogheader.GetEnumerator();
                    if (!header.MoveNext())
                    {
                        Console.WriteLine("NO HEADER FOR DATA");
                        return false;
                    }


                    System.Int32[] lastdata = new System.Int32[TLGdata.datalogdata.Count];
                    for (int i = 0; i < TLGdata.datalogdata.Count; i++)
                        lastdata[i] = ((LogElementType<System.Int32>)TLGdata.datalogdata.ElementAt(i)).values.Last();


                    System.Byte DLVs = reader.ReadByte();

                    for (int n = 0; n < DLVs; n++)
                    {
                        System.Byte DLVn = reader.ReadByte();
                        lastdata[DLVn] = reader.ReadInt32();
                    }

                    for (int i = 0; i < TLGdata.datalogdata.Count; i++)
                        ((LogElementType<System.Int32>)TLGdata.datalogdata.ElementAt(i)).values.Add(lastdata[i]);

                    if (reader.BaseStream.Position >= reader.BaseStream.Length)
                        break;
                }

                if (header.Current.name == "TimeStartDATE" || header.Current.name == "GpsUtcDate")
                {
                    LogElementType<System.String> element = (LogElementType<System.String>)header.Current;
                    DateTime date = new DateTime(1980, 1, 1);
                    ushort days = reader.ReadUInt16();
                    date = date.AddDays(days);
                    element.values.Add(date.ToShortDateString());
                }
                else if (header.Current.name == "TimeStartTOFD" || header.Current.name == "GpsUtcTime")
                {
                    LogElementType<System.String> element = (LogElementType<System.String>)header.Current;
                    DateTime date = new DateTime();
                    uint seconds = reader.ReadUInt32();
                    date = date.AddMilliseconds(seconds);
                    element.values.Add(date.TimeOfDay.ToString());
                }
                else if (header.Current.type == typeof(System.Byte))
                {
                    LogElementType<System.Byte> element = (LogElementType<System.Byte>)header.Current;
                    element.values.Add(reader.ReadByte());
                }
                else if (header.Current.type == typeof(System.Int16))
                {
                    LogElementType<System.Int16> element = (LogElementType<System.Int16>)header.Current;
                    element.values.Add(reader.ReadInt16());
                }
                else if (header.Current.type == typeof(System.Int32))
                {
                    LogElementType<System.Int32> element = (LogElementType<System.Int32>)header.Current;
                    element.values.Add(reader.ReadInt32());
                }
                else if (header.Current.type == typeof(System.UInt16))
                {
                    LogElementType<System.UInt16> element = (LogElementType<System.UInt16>)header.Current;
                    element.values.Add(reader.ReadUInt16());
                }
                else if (header.Current.type == typeof(System.UInt32))
                {
                    LogElementType<System.UInt32> element = (LogElementType<System.UInt32>)header.Current;
                    element.values.Add(reader.ReadUInt32());
                }
                else if (header.Current.type == typeof(System.UInt64))
                {
                    LogElementType<System.UInt64> element = (LogElementType<System.UInt64>)header.Current;
                    element.values.Add(reader.ReadUInt64());
                }
            }

            foreach (var element in TLGdata.datalogdata)
            {
                LogElementType<System.Int32> data = (LogElementType<System.Int32>)element;
                data.values.RemoveAt(0);
            }

            return true;
        }

        // Return difference betweeen angle1 and angle2 wrapped on interval [-pi, pi]
        static double angleDiff(double angle1, double angle2)
        {
            double angle = angle1 - angle2;

            while (angle > Math.PI)
                angle -= 2*Math.PI;

            while (angle < -Math.PI)
                angle += 2*Math.PI;

            return angle;
        }

        bool simulateGeometry()
        {
            // Convert WGS-84 coordinates to ENU coordinates
            List<System.Int32> latitude = ((LogElementType<System.Int32>)TLGdata.datalogheader.Where(header => header.name == "PositionNorth").Single()).values;
            List<System.Int32> longitude = ((LogElementType<System.Int32>)TLGdata.datalogheader.Where(header => header.name == "PositionEast").Single()).values;
            List<System.Int32> height;

            // If data is missing height use 0
            var heightData = TLGdata.datalogheader.Where(header => header.name == "PositionUp").ToList();
            if (heightData.Count > 0)
                height = ((LogElementType<System.Int32>)heightData.First()).values;
            else
                height = new List<System.Int32>(new System.Int32[latitude.Count]);


            List<System.Double> xGNSS = new List<System.Double>();
            List<System.Double> yGNSS = new List<System.Double>();
            List<System.Double> zGNSS = new List<System.Double>();
            List<System.Double> yawGNSS = new List<System.Double>();

            double lat0 = latitude[0] * 0.0000001;
            double lon0 = longitude[0] * 0.0000001;
            double h0 = height[0] * 0.001;

            xGNSS.Add(0.0);
            yGNSS.Add(0.0);
            zGNSS.Add(0.0);
            yawGNSS.Add(0.0);
            bool fillFirsts = true;
            bool reverse = false;
            int reversecount = 0;
            int forwardcount = 0;

            double dx = 0;
            double dy = 0;

            for (int i=1; i<longitude.Count; i++)
            {
                GpsUtils.GeodeticToEnu(latitude[i] * 0.0000001, longitude[i] * 0.0000001, height[i] * 0.001,
                                          lat0, lon0, h0,
                                          out double x, out double y, out double z);

                xGNSS.Add(x);
                yGNSS.Add(y);
                zGNSS.Add(z);

                dx += x - xGNSS[i - 1];
                dy += y - yGNSS[i - 1];

                // Estimate Yaw if there is no measurement
                // have to move enough before heading is estimated
                if ((dx * dx + dy * dy) > 0.01)     // 0.01 --> 0.1m
                {
                    yawGNSS.Add(Math.Atan2(dy, dx));
                    dx = 0;
                    dy = 0;

                    if (fillFirsts)
                    {
                        for (var j = 0; j < i; j++)
                            yawGNSS[j] = yawGNSS[i];
                        fillFirsts = false;
                    }
                    else
                    {
                        if (reverse)
                        {
                            yawGNSS[i] += Math.PI;
                            reversecount++;
                        }
                        else
                            forwardcount++;

                        // yaw is changed to opposite direction? --> tractor is reversing
                        if (Math.Abs(angleDiff(yawGNSS[i], yawGNSS[i - 1])) > Math.PI / 2)
                        {
                            reverse = !reverse;
                            yawGNSS[i] += Math.PI;
                        }
                    }
                }
                else
                {
                    yawGNSS.Add(yawGNSS[i-1]);
                }
            }

            // assume that there is more forward driving that backwards. If not, fix it
            if (reversecount > forwardcount)
                yawGNSS = yawGNSS.Select(yaw => yaw + Math.PI).ToList();

            // apply a filter to prevent zigzagging
            List<System.Double> yawFiltered = new List<double>();
            yawFiltered.Add(yawGNSS[0]);
            yawFiltered.Add(yawGNSS[1]);
            for (int i=2; i< yawGNSS.Count-2; i++)
            {
                double angle = 0;
                for (int j = -2; j < 3; j++)
                    angle += yawGNSS[i] + angleDiff(yawGNSS[i+j], yawGNSS[i]);
                yawFiltered.Add(angle / 5);
            }
            yawFiltered.Add(yawGNSS[yawGNSS.Count - 2]);
            yawFiltered.Add(yawGNSS[yawGNSS.Count - 1]);
            yawGNSS = yawFiltered;

            
            // simulate geometry information
            foreach (var geometry in TLGdata.geometry)
            {
                List<System.Double> yaw = yawGNSS;

                // TODO: There is some bug here. DPD values are not the same as calculated yaw
                /*
                if (geometry.yaw != null)
                {
                    var YawLog = geometry.datalogdata.Where(data => data.DPD.DDI == geometry.yaw.DDI && data.DPD.element == geometry.yaw.element);
                    if (YawLog.Any())
                        yaw = ((LogElementType<System.Int32>)YawLog.First()).values.Select(value => ((double)value) * (Math.PI / 180.0 * 0.001)).ToList();
                }
                */

                LogElementType<System.Int32> LogLat = new LogElementType<System.Int32>("PositionNorth");
                LogElementType<System.Int32> LogLon = new LogElementType<System.Int32>("PositionEast");
                LogElementType<System.Int32> LogUp = new LogElementType<System.Int32>("PositionUp");

                double implementYaw = yaw[0];
                Point CRP = null;

                for (int i = 0; i < xGNSS.Count; i++)
                {
                    // initial position is NRP that is GNSS antenna position
                    Point position = new Point(xGNSS[i], yGNSS[i], zGNSS[i]);

                    // calculate the position of the tractor's DRP
                    position -= geometry.getValues(geometry.TractorNRP, i).RotateZ(yaw[i]);

                    // calculate the position of the tractor's CRP
                    position += geometry.getValues(geometry.TractorCRP, i).RotateZ(yaw[i]);

                    // simulate the implement angle
                    switch(geometry.connection)
                    {
                        case TractorImplementGeometry.ConnectionType.Mounted:
                            implementYaw = yaw[i];
                            break;

                        case TractorImplementGeometry.ConnectionType.Towed:

                            // If distance between DRP and CRP is not positive, it cannot be Towed implement
                            if (CRP != null && geometry.getValues(geometry.ImplementCRP, i).x > 0)
                            {
                                Point speed = position - CRP;
                                double tangentSpeed = speed.DotProduct(new Point(0, 1).RotateZ(implementYaw));
                                implementYaw += tangentSpeed / geometry.getValues(geometry.ImplementCRP, i).x; 
                            }
                            else
                            {
                                implementYaw = yaw[i];
                            }
                            CRP = new Point(position);

                            break;
                    }
                    
                    // calculate the postion of the implement's DRP
                    position -= geometry.getValues(geometry.ImplementCRP, i).RotateZ(implementYaw);

                    // calculate the ERP position
                    position += geometry.getValues(geometry.ImplementERP, i).RotateZ(implementYaw);

                    if (cartesianCoordinates)
                    {
                        LogLat.values.Add((Int32)(position.x * 1000));
                        LogLon.values.Add((Int32)(position.y * 1000));
                        LogUp.values.Add((Int32)(position.z * 1000));
                    }
                    else
                    {
                        // convert ENU coordinates to WGS-84 coordinates 
                        GpsUtils.EnuToGeodetic(position.x, position.y, position.z,
                                                  lat0, lon0, h0,
                                                  out double lat, out double lon, out double h);

                        LogLat.values.Add((Int32)(lat * 10000000));
                        LogLon.values.Add((Int32)(lon * 10000000));
                        LogUp.values.Add((Int32)(h * 1000));
                    }
                }

                // create datalogheader
                geometry.datalogheader.Add(LogLat);
                geometry.datalogheader.Add(LogLon);
                geometry.datalogheader.Add(LogUp);

                foreach (var header in TLGdata.datalogheader)
                    if (header.name != "PositionNorth" && header.name != "PositionEast" && header.name != "PositionUp")
                        geometry.datalogheader.Add(header);
            }

            return true;
        }

        bool convertTaskFile(String filename)
        {
            bool retvalue = true;

            // Read main file and linked xml files

            XDocument ISOTaskFile;
            try
            {
                ISOTaskFile = XDocument.Load(filename);
            }
            catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }


            var XFRlist = ISOTaskFile.Root.Descendants("XFR");
            string directory = System.IO.Path.GetDirectoryName(filename) + "\\";

            foreach (var XFR in XFRlist)
            {
                string file = directory + XFR.Attribute("A").Value + ".xml";

                XDocument EXTFile;
                try
                {
                    EXTFile = XDocument.Load(file);
                }
                catch (System.IO.FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }

                ISOTaskFile.Root.Add(EXTFile.Element("XFC").Elements());
            }

            ISOTaskFile.Root.Descendants("XFR").Remove();

                        
            // extract TASK information
            foreach(var TSK in ISOTaskFile.Root.Descendants("TSK"))
            {
                TLGdata.taskname = TSK.Attribute("B").Value;
                TLGdata.field = ISOTaskFile.Root.Descendants("PFD").Where(pdf => pdf.Attribute("A").Value == TSK.Attribute("E").Value).Single().Attribute("C").Value;
                TLGdata.farm = ISOTaskFile.Root.Descendants("FRM").Where(frm => frm.Attribute("A").Value == TSK.Attribute("D").Value).Single().Attribute("B").Value;
                
                List<string> devicelist = TSK.Elements("DAN").Attributes("C").Select(attr => attr.Value).ToList();
                TLGdata.devices = ISOTaskFile.Root.Descendants("DVC").Where(dvc => devicelist.Contains(dvc.Attribute("A").Value)).Attributes("B").Select(attr => attr.Value).ToList();

                // extract geometry information
                retvalue = extractGeometryInformation(TSK) && retvalue;


                foreach (var TLG in TSK.Descendants("TLG"))
                {
                    // extract timelog  
                    if (!extractTimelog(TLG, directory))
                    {
                        retvalue = false;

                        // clear current TLG data
                        TLGdata.datalogheader.Clear();
                        TLGdata.datalogdata.Clear();

                        foreach (var geometry in TLGdata.geometry)
                        {
                            geometry.datalogheader.Clear();
                            geometry.datalogdata.Clear();
                        }
                            
                        continue;
                    }


                    // simulate geometry
                    Console.WriteLine("*****  Simulate  *******");
                    retvalue = simulateGeometry() && retvalue;


                    // write data out
                    Console.WriteLine("** Write output files **");
                    foreach (var geometry in TLGdata.geometry)
                    {
                        if (geometry.datalogdata.Any())
                        {
                            if (outputType == OutputType.GML)
                            {
                                if (!writeGMLFile(generateName(TLG.Attribute("A").Value + " " + geometry.description) + ".GML", geometry.datalogheader, geometry.datalogdata))
                                {
                                    Console.WriteLine("ERROR IN WRITING GML");
                                    retvalue = false;
                                }
                            }
                            else if (outputType == OutputType.CSV)
                            {
                                if (!writeCSVFile(generateName(TLG.Attribute("A").Value + " " + geometry.description) + ".CSV", geometry.datalogheader, geometry.datalogdata))
                                {
                                    Console.WriteLine("ERROR IN WRITING GML");
                                    retvalue = false;
                                }
                            }
                        }
                    }


                    // clear current TLG data
                    TLGdata.datalogheader.Clear();
                    TLGdata.datalogdata.Clear();

                    foreach (var geometry in TLGdata.geometry)
                    {
                        geometry.datalogheader.Clear();
                        geometry.datalogdata.Clear();
                    }
                }
            }

            return retvalue;
        }

        bool writeGMLFile(String filename, List<LogElement> datalogheader, List<LogElement> datalogdata)
        {
            XNamespace gml = "http://www.opengis.net/gml";
            XNamespace tnt = "http://www.microimages.com/TNT";

            LogElementType<System.Int32> longitude = (LogElementType<System.Int32>)datalogheader.Where(header => header.name == "PositionEast").Single();
            LogElementType<System.Int32> latitude = (LogElementType<System.Int32>)datalogheader.Where(header => header.name == "PositionNorth").Single();
            LogElementType<System.Int32> height;
            
            // If data is missing height use 0
            var heightData = datalogheader.Where(header => header.name == "PositionUp").ToList();
            if (heightData.Count > 0)
            {
                height = (LogElementType<System.Int32>)heightData.First();
            }
            else 
            { 
                height = new LogElementType<System.Int32>("height");
                height.values = new List<System.Int32>(new System.Int32[latitude.values.Count]);
            }
            string boundingbox = longitude.values.Min() * 0.0000001 + "," + latitude.values.Min() * 0.0000001 + " " + longitude.values.Max() * 0.0000001 + "," + latitude.values.Max() * 0.0000001;

            XDocument GMLFile = new XDocument(
                new XElement(tnt + "FeatureCollection",
                    new XAttribute(XNamespace.Xmlns + "tnt", tnt),
                    new XAttribute(XNamespace.Xmlns + "gml", gml),
                    new XElement(gml + "boundedBy",
                        new XAttribute("srsName", "EPSG:4326"),
                        new XElement(gml + "coordinates",
                            boundingbox
                            )
                        )
                    )
                );

            int index = 0;

            while (index < datalogheader.ElementAt(0).getSize())
            {
                XElement datapoint = new XElement(tnt + (TLGdata.taskname.Replace("+", "_").Replace(" ", "_").Replace("&", "_") + "_point"));
                
                foreach (var header in datalogheader)
                {
                    if (header.name != "PositionNorth" && header.name != "PositionEast" && header.name != "PositionUp")
                        datapoint.Add(new XElement(tnt + header.name.Replace(" ", "_"), header.getValueString(index)));
                }
                    
                
                foreach (var element in datalogdata)
                {
                    string elName = element.name.Replace(" ", "_").Replace("&", "_").Replace("(", "_").Replace(")", "_");
                    if (Regex.IsMatch(elName, @"^[0-9]"))
                        elName = "X" + elName;
                    datapoint.Add(new XElement(tnt + elName, element.getValueString(index)));
                }
                

                datapoint.Add(new XElement(tnt + "_POINT_",
                        new XElement(gml + "Point",
                            new XAttribute("srsName", "EPSG:4326"),
                            new XElement(gml + "coordinates",
                                    longitude.values.ElementAt(index) * 0.0000001 + "," +
                                    latitude.values.ElementAt(index) * 0.0000001 + "," +
                                    height.values.ElementAt(index) * 0.001
                                )
                            )    
                    )
                );

                GMLFile.Root.Add(new XElement(gml + "featureMember", datapoint));

                index++;
            }

            GMLFile.Save(filename);
            
            return true;
        }



        bool writeCSVFile(String filename, List<LogElement> datalogheader, List<LogElement> datalogdata)
        {
            
            StreamWriter file = new StreamWriter(filename);

            // Write variable names to the first line
            foreach (var header in datalogheader)
            {
                file.Write(header.name + "; ");
            }

            foreach (var element in datalogdata)
            {
                string elName = element.name.Replace(" ", "_").Replace("&", "_").Replace("(", "_").Replace(")", "_");
                if (Regex.IsMatch(elName, @"^[0-9]"))
                    elName = "X" + elName;

                file.Write(elName + "; ");
            }
            file.Write("\n");


            // Write values
            int index = 0;
            while (index < datalogheader.ElementAt(0).getSize())
            {

                foreach (var header in datalogheader)
                {
                    file.Write(header.getValueString(index) + "; ");
                }


                foreach (var element in datalogdata)
                {
                    file.Write(element.getValueString(index) + "; ");
                }

                file.Write("\n");
                index++;
            }
                        

            return true;
        }
    }
}
