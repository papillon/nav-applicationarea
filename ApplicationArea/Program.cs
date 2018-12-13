using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Dynamics.Nav.MetaModel;
using Microsoft.Dynamics.Nav.MetaMetaModel;
using Microsoft.Dynamics.Nav.Model.IO.Txt;

namespace Singhammer.SITE
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine(@"Usage: ApplicationArea.exe directory\*.txt -set MyArea [-minId id] [-maxId id]");
                Console.WriteLine(@"Usage: ApplicationArea.exe directory\*.txt -reset MyArea [-minId id] [-maxId id]");
                return (-1);
            }

            string mode = "";
            string sourcePath = Path.GetDirectoryName(args[0]);
            string sourcePattern = Path.GetFileName(args[0]);
            string area = "";
            int minId = 0;
            int maxId = 0;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-set":
                    case "-reset":
                        mode = args[i];
                        area = "#" + Path.GetFileName(args[i + 1]);
                        i++;
                        break;
                    case "-minid":
                        minId = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "-maxid":
                        maxId = int.Parse(args[i + 1]);
                        i++;
                        break;
                }
            }

            switch (mode)
            {
                case "-set":
                    ProcessFiles(sourcePath, sourcePattern, area, true, minId, maxId);
                    break;
                case "-reset":
                    ProcessFiles(sourcePath, sourcePattern, area, false, minId, maxId);
                    break;
            }
            return 0;
        }

        /// <summary>  
        ///  Read all objects in a file
        /// </summary> 
        /// <param name="fileName">The text file. May contain only one or multiple objects</param>
        /// <returns>List of ApplicationObjects</returns>
        public static List<ApplicationObject> ReadNavObjects(string fileName)
        {
            TxtFileModelInfo modelInfo = new TxtFileModelInfo();
            TxtImporter importer = new TxtImporter(modelInfo);
            try
            {
                using (var instream = new FileStream(fileName, FileMode.Open))
                {
                    List<ApplicationObject> objects = importer.ImportFromStream(instream);
                    if (objects != null && objects.Count > 0)
                        return objects;
                    else
                    {
                        Console.WriteLine(@"Object could not be read from file {0}", fileName);
                    }
                }
            }
            catch (Microsoft.Dynamics.Nav.Model.IO.Txt.TxtImportException e)
            {
                Console.WriteLine(@"Exception while reading {0}: {1}", fileName, e.Message);
                Console.WriteLine(@"Source line {0}, col {1}: {2}", e.LineNo, e.LinePos, e.Line);
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(@"Exception while reading {0}: {1}", fileName, e.Message);
            }
            return new List<ApplicationObject>();
        }

        /// <summary>  
        ///  Save all objects in the list to a file
        /// </summary> 
        /// <param name="fileName">The text file where objects are written to</param>
        /// <param name="objects">A List of ApplicationObjects</param>
        public static void SaveNavObjects(string fileName, List<ApplicationObject> objects)
        {
            TxtFileModelInfo modelInfo = new TxtFileModelInfo();
            TxtExporter export = new TxtExporter(modelInfo);
            try
            {
                using (var outstream = new FileStream(fileName, FileMode.Create))
                {
                    foreach (var obj in objects)
                    {
                        export.ExportObject(obj, outstream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"Exception while writing {0}: {1}", fileName, e.Message);
            }
        }

        /// <summary>  
        ///  Loop over several files and set or unset the ApplicationArea property
        /// </summary> 
        /// <param name="sourcePath">The directory where the text files are stored</param>
        /// <param name="sourcePattern">Specifies what files to process (e.g. MyObject.txt or *.txt)</param>
        /// <param name="area">Specifies the ApplicationArea to set or unset</param>
        /// <param name="set">True if area should be set, false if it should be removed</param>
        /// <param name="minId">Minimum ID of controls that should be changed</param>
        /// <param name="maxId">Maximum ID of controls that should be changed</param>
        static void ProcessFiles(string sourcePath, string sourcePattern, string area, bool set, int minId, int maxId)
        {
            int objectCount = 0, controlCount = 0;
            string[] files = Directory.GetFiles(sourcePath, sourcePattern, SearchOption.AllDirectories);
            foreach (string file in files)
            {
                bool dirty = false;
                List<ApplicationObject> objects = ReadNavObjects(file);
                foreach (var obj in objects)
                {
                    var objDirty = false;

                    // Select the elements that should be processed in this object
                    IEnumerable<IElement> elements =
                        from element in obj.GetElements()
                        where (element.ElementTypeInfo.ElementType == ElementType.Action && element.GetStringProperty(PropertyType.ActionType) == "Action") ||
                              (element.ElementTypeInfo.ElementType == ElementType.Control && element.GetStringProperty(PropertyType.ControlType) == "Field") ||
                              (element.ElementTypeInfo.ElementType == ElementType.Control && element.GetStringProperty(PropertyType.ControlType) == "Part") ||
                              (element.ElementTypeInfo.ElementType == ElementType.MenuNode && element.GetStringProperty(PropertyType.MenuNodeType) == "MenuItem" &&
                                element.GetStringProperty(PropertyType.RunObjectType) != null)
                        select element;


                    // Reduce set to applicable IDs
                    if (minId > 0)
                        elements =
                            from element in elements
                            where element.Id >= minId
                            select element;
                    if (maxId > 0)
                        elements =
                            from element in elements
                            where element.Id <= maxId
                            select element;

                    foreach (IElement element in elements)
                    {
                        try
                        {
                            string value = element.GetStringProperty(PropertyType.ApplicationArea);
                            if (set)
                            {
                                if (String.IsNullOrEmpty(value))
                                {
                                    element.SetStringProperty(PropertyType.ApplicationArea, area);
                                    objDirty = true;
                                    controlCount++;
                                }
                            }
                            else
                            {
                                if (value == area)
                                {
                                    element.ClearProperty(PropertyType.ApplicationArea);
                                    objDirty = true;
                                    controlCount++;
                                }
                            }
                        }
                        catch (Exception) { }
                    }

                    // Set ApplicationArea on Page and Report Objects (only if UsageCategory is set)
                    if (obj.ElementTypeInfo.ElementType == ElementType.Page || obj.ElementTypeInfo.ElementType == ElementType.Report)
                    {
                        string usageCategoryText = obj.RootElement.GetStringProperty(PropertyType.UsageCategory);
                        string applicationAreaText = obj.RootElement.GetStringProperty(PropertyType.ApplicationArea);
                        if (set)
                        {
                            if (!String.IsNullOrEmpty(usageCategoryText) && String.IsNullOrEmpty(applicationAreaText))
                            {
                                obj.RootElement.SetStringProperty(PropertyType.ApplicationArea, area);
                                objDirty = true;
                                controlCount++;
                            }
                        }
                        else
                        {
                            if (applicationAreaText == area)
                            {
                                obj.RootElement.ClearProperty(PropertyType.ApplicationArea);
                                objDirty = true;
                                controlCount++;
                            }
                        }
                    }

                    if (objDirty)
                    {
                        dirty = true;
                        objectCount++;
                    }
                }
                if (dirty)
                {
                    SaveNavObjects(file, objects);
                }
            }
            Console.WriteLine("{0} object(s) with {1} controls(s) processed.", objectCount, controlCount);
        }
    }
}


