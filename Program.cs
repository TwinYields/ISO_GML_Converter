
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
    public class LogElement
    {
        public string name;
        public Type type;

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

    public class TimeLogData
    {
        public string taskname;
        public string field;
        public string farm;
        public List<string> devices;

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
                                   
            if (!readTaskFile(taskfilename))
            {
                Console.WriteLine("ERROR IN READING FILE");
                return;
            }
        }

        string generateName()
        {
            string name = TLGdata.farm + "_" + TLGdata.field + "_" + TLGdata.taskname + "_";

            LogElementType<System.String> TimeStartDATE = (LogElementType<System.String>)TLGdata.datalogheader.Where(val => val.name == "TimeStartDATE").Single();
            name += TimeStartDATE.values.First();

            LogElementType<System.String> TimeStartTOFD = (LogElementType<System.String>)TLGdata.datalogheader.Where(val => val.name == "TimeStartTOFD").Single();
            name += "_" + TimeStartTOFD.values.First().Replace(":", ".");

            char[] invalids = System.IO.Path.GetInvalidFileNameChars();
            name = String.Join("", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

            return name;
        }

        bool readTaskFile(String filename)
        {
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

            
            // Read binary timelog files 

            foreach(var TSK in ISOTaskFile.Root.Descendants("TSK"))
            {
                TLGdata.taskname = TSK.Attribute("B").Value;
                TLGdata.field = ISOTaskFile.Root.Descendants("PFD").Where(pdf => pdf.Attribute("A").Value == TSK.Attribute("E").Value).Single().Attribute("C").Value;
                TLGdata.farm = ISOTaskFile.Root.Descendants("FRM").Where(frm => frm.Attribute("A").Value == TSK.Attribute("D").Value).Single().Attribute("B").Value;
                
                List<string> devicelist = TSK.Elements("DAN").Attributes("C").Select(attr => attr.Value).ToList();
                TLGdata.devices = ISOTaskFile.Root.Descendants("DVC").Where(dvc => devicelist.Contains(dvc.Attribute("A").Value)).Attributes("B").Select(attr => attr.Value).ToList();
                
                foreach(var TLG in TSK.Descendants("TLG"))
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


                    foreach(var PTN in TLGFile.Element("TIM").Descendants("PTN"))
                    {
                        if(PTN.Attribute("A") != null && PTN.Attribute("A").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionNorth"));
                        if(PTN.Attribute("B") != null && PTN.Attribute("B").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionEast"));
                        if (PTN.Attribute("C") != null && PTN.Attribute("C").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionUp"));
                        if(PTN.Attribute("D") != null && PTN.Attribute("D").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Byte>("PositionStatus"));
                        if(PTN.Attribute("E") != null && PTN.Attribute("E").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("PDOP"));
                        if(PTN.Attribute("F") != null && PTN.Attribute("F").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("HDOP"));
                        if(PTN.Attribute("G") != null && PTN.Attribute("G").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Byte>("NumberOfSatellites"));
                        if(PTN.Attribute("H") != null && PTN.Attribute("H").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcTime"));
                        if(PTN.Attribute("I") != null && PTN.Attribute("I").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcDate"));
                    }

                   
                    foreach (var DLV in TLGFile.Element("TIM").Descendants("DLV"))
                    {
                        string ProcessDataDDI = DLV.Attribute("A").Value;
                        string DeviceElementIdRef = DLV.Attribute("C").Value;

                        var DET = ISOTaskFile.Root.Descendants("DET").Where(det => det.Attribute("A").Value == DeviceElementIdRef).Single();
                        try
                        {
                            var DOR = DET.Descendants("DOR").Attributes("A").Select(atr => atr.Value).ToList();
                            var DPD = ISOTaskFile.Root.Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && dpd.Attribute("B").Value == ProcessDataDDI).Single();

                            string DVCdesignator = DET.Parent.Attribute("B").Value;  // Note! optional attribute
                            string DETdesignator = DET.Attribute("D").Value; // Note! optional attribute
                            string DPDdesignator = DPD.Attribute("E").Value; // Note! optional attribute --> Use DDI description instead

                            LogElementType<System.Int32> logelement = new LogElementType<System.Int32>(DVCdesignator + "_" + DETdesignator + "_" + DPDdesignator);
                            try
                            {
                                logelement.values.Add(System.Int32.Parse(DLV.Attribute("B").Value));
                            }
                            catch(System.FormatException)
                            {
                                logelement.values.Add(0);
                            }
                            TLGdata.datalogdata.Add(logelement);                                
                        }
                        catch (System.InvalidOperationException)
                        {
                            Console.WriteLine("Process data description not found!");
                            
                            LogElementType<System.Int32> logelement = new LogElementType<System.Int32>(DeviceElementIdRef + "_" + ProcessDataDDI);
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
                    }


                    Console.WriteLine(TLG.Attribute("A").Value);
                    Console.WriteLine("******* Header: **********");
                    foreach (var element in TLGdata.datalogheader)
                    {
                        Console.WriteLine(element.name);
                    }
                    Console.WriteLine("******* Data: **********");
                    foreach (var element in TLGdata.datalogdata)
                    {
                        Console.WriteLine(element.name);
                    }
                    Console.WriteLine("==============================================");
                                            
                    // ready binary
                    string binary_file = directory + TLG.Attribute("A").Value + ".bin";

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
                            DateTime date = new DateTime(1980,1,1);
                            date = date.AddDays(reader.ReadUInt16());
                            element.values.Add(date.ToShortDateString());
                        }
                        else if (header.Current.name == "TimeStartTOFD" || header.Current.name == "GpsUtcTime")
                        {
                            LogElementType<System.String> element = (LogElementType<System.String>)header.Current;
                            DateTime date = new DateTime();
                            date = date.AddMilliseconds(reader.ReadUInt32());
                            element.values.Add(date.TimeOfDay.ToString());
                        }
                        else if (header.Current.type == typeof(System.Byte))
                        {
                            LogElementType<System.Byte> element = (LogElementType<System.Byte>)header.Current;
                            element.values.Add(reader.ReadByte());
                        }
                        else if(header.Current.type == typeof(System.Int16))
                        {
                            LogElementType<System.Int16> element = (LogElementType<System.Int16>)header.Current;
                            element.values.Add(reader.ReadInt16());
                        }
                        else if(header.Current.type == typeof(System.Int32))
                        {
                            LogElementType<System.Int32> element = (LogElementType<System.Int32>)header.Current;
                            element.values.Add(reader.ReadInt32());
                        }
                        else if(header.Current.type == typeof(System.UInt16))
                        {
                            LogElementType<System.UInt16> element = (LogElementType<System.UInt16>)header.Current;
                            element.values.Add(reader.ReadUInt16());
                        }
                        else if(header.Current.type == typeof(System.UInt32))
                        {
                            LogElementType<System.UInt32> element = (LogElementType<System.UInt32>)header.Current;
                            element.values.Add(reader.ReadUInt32());
                        }
                        else if(header.Current.type == typeof(System.UInt64))
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

                    if (outputType == OutputType.GML)
                    {
                        if (!writeGMLFile(generateName() + TLG.Attribute("A").Value + ".GML"))
                        {
                            Console.WriteLine("ERROR IN WRITING GML");
                        }
                    }
                    else if (outputType == OutputType.CSV)
                    {
                        if (!writeCSVFile(generateName() + TLG.Attribute("A").Value + ".CSV"))
                        {
                            Console.WriteLine("ERROR IN WRITING GML");
                        }
                    }

                    TLGdata.datalogheader.Clear();
                    TLGdata.datalogdata.Clear();
                }
            }

            return true;
        }

        bool writeGMLFile(String filename)
        {
            XNamespace gml = "http://www.opengis.net/gml";
            XNamespace tnt = "http://www.microimages.com/TNT";

            LogElementType<System.Int32> longitude = (LogElementType<System.Int32>)TLGdata.datalogheader.Where(header => header.name == "PositionEast").Single();
            LogElementType<System.Int32> latitude = (LogElementType<System.Int32>)TLGdata.datalogheader.Where(header => header.name == "PositionNorth").Single();
            LogElementType<System.Int32> height;
            
            // If data is missing height use 0
            var heightData = TLGdata.datalogheader.Where(header => header.name == "PositionUp").ToList();
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

            while (index < TLGdata.datalogheader.ElementAt(0).getSize())
            {
                XElement datapoint = new XElement(tnt + (TLGdata.taskname.Replace("+", "_").Replace(" ", "_").Replace("&", "_") + "_point"));
                
                foreach (var header in TLGdata.datalogheader)
                {
                    if (header.name != "PositionNorth" && header.name != "PositionEast" && header.name != "PositionUp")
                        datapoint.Add(new XElement(tnt + header.name.Replace(" ", "_"), header.getValueString(index)));
                }
                    
                
                foreach (var element in TLGdata.datalogdata)
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



        bool writeCSVFile(String filename)
        {
            
            StreamWriter file = new StreamWriter(filename);

            // Write variable names to the first line
            foreach (var header in TLGdata.datalogheader)
            {
                file.Write(header.name + "; ");
            }

            foreach (var element in TLGdata.datalogdata)
            {
                string elName = element.name.Replace(" ", "_").Replace("&", "_").Replace("(", "_").Replace(")", "_");
                if (Regex.IsMatch(elName, @"^[0-9]"))
                    elName = "X" + elName;

                file.Write(elName + "; ");
            }
            file.Write("\n");


            // Write values
            int index = 0;
            while (index < TLGdata.datalogheader.ElementAt(0).getSize())
            {

                foreach (var header in TLGdata.datalogheader)
                {
                    file.Write(header.getValueString(index) + "; ");
                }


                foreach (var element in TLGdata.datalogdata)
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
