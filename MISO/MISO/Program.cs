/*
Copyright (c) 2009, Serials Solutions
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the <ORGANIZATION> nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using SushiLibrary;
using SushiLibrary.Validation;

namespace MISO
{
    class Program
    {
        // constants
        private const string CounterSchemaURL = "http://www.niso.org/schemas/sushi/counter_sushi3_0.xsd";
        private static readonly string[] DateTimeFormats = { "yyyyMM" };
        private const string CommandParameterMessage = @"Parameters are:
MISO.EXE [-v] [filename] [-d] [start] [end] [-l] [Library codes separated by commas]
-v: Validate Mode: Validates a specified file or reports from the sushi server with the counter-sushi schema

-s: Strict Validation Mode: Validates a specified file or reports from the sushi server with the schema and custom validation rules

-d: Specify Sushi request start and end dates
    [start]: start date in yyyymm format
    [end]: end date in yyyymm format
    By default, it will automatically request the previous month from the current date.

-l: Specify Library Codes

-x: Save Request Response as XML without converting to csv";

        private static readonly char[] DELIM = { ',' };

        // global variables to MISO
        private static DateTime StartDate;
        private static DateTime EndDate;
        private static string RequestTemplate;
        private static bool XmlMode = false;
        private static bool ValidateMode = false;
        private static bool StrictValidateMode = false;
        private static readonly XmlReaderSettings XmlReaderSettings = new XmlReaderSettings();

        // lookup table to find month data
        private static Dictionary<string, string> MonthData;

        private static TextWriter _errorFile = null;
        private static TextWriter ErrorFile
        {
            get
            {
                // create the file
                if (_errorFile == null)
                {
                    _errorFile = new StreamWriter(string.Format("Error_{0}.txt", ErrorDate));
                }
                return _errorFile;
            }
        }

        //validation mode stuff
        private static bool IsValid = true;

        private static void CounterV3ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            IsValid = false;
            Console.WriteLine("Validation event\n" + args.Message);

        }

        private static string ErrorDate
        {
            get { return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"); }
        }

        static void Main(string[] args)
        {
            #region Process Arguements
            string validateFile = string.Empty;
            bool specifiedLibCodes = false;
            string start = string.Empty;
            string end = string.Empty;
            string libCodeStr = string.Empty;

            try
            {

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {

                        case "-v":
                            ValidateMode = true;
                            if (i + 1 < args.Length)
                            {
                                validateFile = args[i + 1];
                            }
                            break;
                        case "-d":
                            if (i + 1 < args.Length)
                            {
                                start = args[i + 1];
                            }
                            if (i + 2 < args.Length)
                            {
                                end = args[i + 2];
                            }
                            break;
                        case "-l":
                            specifiedLibCodes = true;
                            if (i + 1 >= args.Length)
                            {
                                throw new ArgumentException("File not specified for validation mode");
                            }
                            libCodeStr = args[i + 1];
                            break;
                        case "-x":
                            XmlMode = true;
                            break;
                        case "-h":
                            Console.WriteLine(CommandParameterMessage);
                            System.Environment.Exit(-1);
                            break;
                        case "-s":
                            StrictValidateMode = true;
                            if (i + 1 < args.Length)
                            {
                                validateFile = args[i + 1];
                            }
                            break;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion

            FileStream sushiConfig = new FileStream("sushiconfig.csv", FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(sushiConfig);

            //initiate validation
            XmlSchema CounterSushiSchema = XmlSchema.Read(new XmlTextReader(CounterSchemaURL), new ValidationEventHandler(CounterV3ValidationEventHandler));
            XmlReaderSettings.ValidationType = ValidationType.Schema;
            XmlReaderSettings.Schemas.Add(CounterSushiSchema);
            XmlReaderSettings.ValidationEventHandler += new ValidationEventHandler(CounterV3ValidationEventHandler);

            try
            {

            #region Initialize
                // validate file mode
                if ((ValidateMode || StrictValidateMode) && !string.IsNullOrEmpty(validateFile))
                {
                    using (XmlReader xmlReader = XmlReader.Create(new XmlTextReader(validateFile), XmlReaderSettings))
                    {

                        // Read XML to the end
                        try
                        {
                            while (xmlReader.Read())
                            {
                                // just read through file to trigger any validation errors
                            }

                            Console.WriteLine("\nFinished validating XML file....");
                        }
                        catch (FileNotFoundException e)
                        {
                            Console.WriteLine(e.Message);
                            System.Environment.Exit(-1);
                        }
                    }

                    if (StrictValidateMode)
                    {
                        var file = new FileStream(validateFile, FileMode.Open, FileAccess.Read);
                        var xmlFile = new XmlDocument();
                        xmlFile.Load(file);

                        SushiReport report = ReportLoader.LoadCounterReport(xmlFile);

                        var validator = new CounterValidator();
                        if (!validator.Validate(report))
                        {
                            IsValid = false;

                            foreach (var error in validator.ErrorMessage)
                            {
                                Console.WriteLine(error);
                            }
                        }

                    }

                    if (IsValid)
                    {
                        Console.WriteLine("Document is valid");
                    }
                    else
                    {
                        Console.WriteLine("Document is invalid");
                    }
                }

                else
                {

                    string[] header = sr.ReadLine().Split(DELIM);

                    DateTime startMonth = DateTime.Now.AddMonths(-1);
                    DateTime endMonth = DateTime.Now.AddMonths(-1);

                    
                    if (!string.IsNullOrEmpty(start))
                    {
                        startMonth = DateTime.ParseExact(start, DateTimeFormats, null,
                                                                  DateTimeStyles.None);
                    }
                    if (!string.IsNullOrEmpty(end))
                    {
                        endMonth = DateTime.ParseExact(end, DateTimeFormats, null,
                                                                DateTimeStyles.None);
                    }

                    if (endMonth < startMonth)
                    {
                        throw new ArgumentException("End date is before start date.");
                    }

                    StartDate = new DateTime(startMonth.Year, startMonth.Month, 1);
                    EndDate = new DateTime(endMonth.Year, endMonth.Month,
                                           DateTime.DaysInMonth(endMonth.Year, endMonth.Month));

                    FileStream requestTemplate = new FileStream("SushiSoapEnv.xml", FileMode.Open, FileAccess.Read);
                    StreamReader reader = new StreamReader(requestTemplate);
                    RequestTemplate = reader.ReadToEnd();
                    reader.Close();

                    #endregion

                    Dictionary<string, string> libCodeMap = null;
                    if (specifiedLibCodes)
                    {
                        libCodeMap = new Dictionary<string, string>();

                        string[] libCodes = libCodeStr.Split(DELIM);
                        foreach (string libCode in libCodes)
                        {
                            libCodeMap.Add(libCode.ToUpper(), string.Empty);
                        }
                    }

                    string buffer;
                    for (int lineNum = 1; (buffer = sr.ReadLine()) != null; lineNum++)
                    {
                        string[] fields = buffer.Split(DELIM);

                        if (libCodeMap == null || libCodeMap.ContainsKey(fields[0].ToUpper()))
                        {
                            if (fields.Length < 13)
                            {
                                ErrorFile.WriteLine(string.Format("{0}: Line {1} has insufficient data", ErrorDate,
                                                                  lineNum));
                            }
                            else
                            {
                                //loop through report types in header
                                for (int i = 9; i < 14; i++)
                                {
                                    try
                                    {
                                        if (fields[i].ToLower().StartsWith("y"))
                                        {
                                            ProcessSushiRequest(header[i], fields);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorFile.WriteLine(
                                            string.Format(
                                                "{0}: Exception occurred processing line {1} for report type {2}",
                                                ErrorDate,
                                                lineNum, header[i]));
                                        ErrorFile.WriteLine(ex.Message);
                                        ErrorFile.Write(ex.StackTrace);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sr.Close();
                sushiConfig.Close();
                if (_errorFile != null)
                {
                    _errorFile.Close();
                }
            }
        }

        /// <summary>
        /// Make the request to the sushi server with the given request
        /// </summary>
        /// <param name="reqDoc"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string CallSushiServer(XmlDocument reqDoc, string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Headers.Add("SOAPAction", "\"SushiService:GetReportIn\"");

            ServicePointManager.ServerCertificateValidationCallback += customXertificateValidation;

            req.ContentType = "text/xml;charset=\"utf-8\"";
            req.Accept = "text/xml";
            req.Method = "POST";
            req.Timeout = Int32.MaxValue;
            Stream stm = req.GetRequestStream();
            reqDoc.Save(stm);
            stm.Close();

            WebResponse resp = req.GetResponse();

            StreamReader SReader= new StreamReader(resp.GetResponseStream());
            return SReader.ReadToEnd();
        }

        private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            //TODO just accept the cert for now since they usually tend to be unregistered
            return true;

        }
        
        /// <summary>
        /// Send sushi request and convert to csv
        /// </summary>
        /// <param name="reportType"></param>
        /// <param name="fields"></param>
        private static void ProcessSushiRequest(string reportType, string[] fields)
        {
            string fileName = string.Format("{0}_{1}_{2}_{3}_{4}", fields[1], fields[0], StartDate.ToString("yyyyMM"), EndDate.ToString("yyyyMM"), reportType);

            fileName = XmlMode ? fileName + ".xml" : fileName + ".csv";

            string startDateStr = StartDate.Date.ToString("yyyy-MM-dd");
            string endDateStr = EndDate.Date.ToString("yyyy-MM-dd");

            XmlDocument reqDoc = new XmlDocument();
            if (fields.Length == 16 && !string.IsNullOrEmpty(fields[14]) && !string.IsNullOrEmpty(fields[15]))
            {
                // Load WSSE fields for Proquest
                FileStream wsSecurityFile = new FileStream("WSSecurityPlainText.xml", FileMode.Open, FileAccess.Read);
                StreamReader reader = new StreamReader(wsSecurityFile);
                string wsSecuritySnippet = string.Format(reader.ReadToEnd(), fields[14], fields[15]);
                reader.Close();
                reqDoc.LoadXml(
                    string.Format(RequestTemplate, fields[4], fields[5], fields[6], fields[7],
                                  fields[8], reportType, fields[2], startDateStr, endDateStr, wsSecuritySnippet));
            }
            else
            {
                reqDoc.LoadXml(
                    string.Format(RequestTemplate, fields[4], fields[5], fields[6], fields[7],
                                  fields[8], reportType, fields[2], startDateStr, endDateStr, string.Empty));
            }


            string resonseString = CallSushiServer(reqDoc, fields[3]);
            if (XmlMode)
            {
                StreamWriter sw = new StreamWriter(fileName);
                sw.Write(resonseString);
                sw.Flush();
                sw.Close();
                return; // parsing and conversion to csv is unnecessary
            }

            XmlDocument sushiDoc = new XmlDocument();
            sushiDoc.LoadXml(resonseString);

            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(sushiDoc.NameTable);
            // Proquest Error
            xmlnsManager.AddNamespace("s", "http://www.niso.org/schemas/sushi");
            XmlNode exception = sushiDoc.SelectSingleNode("//s:Exception", xmlnsManager);

            if (exception != null && exception.HasChildNodes)
            {
                Console.WriteLine(string.Format("Exception detected for report of type {0} for Provider: {1}\nPlease see error log for more details.", reportType, fields[1]));
                throw new XmlException(
                    string.Format("Report returned Exception: Number: {0}, Severity: {1}, Message: {2}",
                    exception.SelectSingleNode("s:Number", xmlnsManager).InnerText, exception.SelectSingleNode("s:Severity", xmlnsManager).InnerText, exception.SelectSingleNode("s:Message", xmlnsManager).InnerText));
            }

            SushiReport sushiReport = ReportLoader.LoadCounterReport(sushiDoc);

            if (ValidateMode || StrictValidateMode)
            {
                IsValid = true;

                MemoryStream ms = new MemoryStream();
                sushiDoc.Save(ms);
                ms.Position = 0;
                using (XmlReader xmlReader = XmlReader.Create(new XmlTextReader(ms), XmlReaderSettings))
                {

                    // Read XML to the end
                    while (xmlReader.Read())
                    {
                        // just read through file to trigger any validation errors
                    }
                }

                if (StrictValidateMode)
                {
                    var validator = new CounterValidator();
                    if (!validator.Validate(sushiReport))
                    {
                        IsValid = false;

                        foreach (var error in validator.ErrorMessage)
                        {
                            Console.WriteLine(error);
                        }
                    }
                }

                Console.WriteLine(string.Format("Finished validation Counter report of type {0} for Provider: {1}", reportType, fields[1]));
                if (IsValid)
                {
                    Console.WriteLine("Document is valid");
                }
                else
                {
                    Console.WriteLine("Document is invalid");
                }
            }

            TextWriter tw = new StreamWriter(fileName);
            StringBuilder header;

            Console.WriteLine("Parsing report of type: " + reportType);

            switch(reportType)
            {
                case "JR1":
                    tw.WriteLine("Journal Report 1 (R2),Number of Successful Full-Text Article Requests By Month and Journal");
                    tw.WriteLine(fields[0]);
                    tw.WriteLine("Date run:");
                    tw.WriteLine(DateTime.Now.ToString("yyyy-M-d"));

                    // construct header
                    header = new StringBuilder(",Publisher,Platform,Print ISSN,Online ISSN");
                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        header.Append(",");
                        header.Append(currMonth.ToString("MMM-yy"));
                    }

                    header.Append(",YTD Total,YTD HTML,YTD PDF");
                    tw.WriteLine(header);

                    ParseJR1v3(sushiReport, tw);

                    tw.Close();

                    break;
                case "DB1":
                    tw.WriteLine("Database Report 1 (R2),Total Searches and Sessions by Month and Database");
                    tw.WriteLine(fields[0]);
                    tw.WriteLine("Date run:");
                    tw.WriteLine(DateTime.Now.ToString("yyyy-M-d"));

                    // construct header
                    header = new StringBuilder(",Publisher,Platform,");
                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        header.Append(",");
                        header.Append(currMonth.ToString("MMM-yy"));
                    }

                    header.Append(",YTD Total");
                    tw.WriteLine(header);


                    ParseDB1v3(sushiDoc, tw);

                    tw.Close();
                    break;
                case "DB3":
                    tw.WriteLine("Database Report 3 (R2),Total Searches and Sessions by Month and Service");
                    tw.WriteLine(fields[0]);
                    tw.WriteLine("Date run:");
                    tw.WriteLine(DateTime.Now.ToString("yyyy-M-d"));

                    // construct header
                    header = new StringBuilder(",Platform,");
                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        header.Append(",");
                        header.Append(currMonth.ToString("MMM-yy"));
                    }

                    header.Append(",YTD Total");
                    tw.WriteLine(header);

                    ParseDB3v3(sushiDoc, tw);

                    tw.Close();
                    break;
                default:
                    ErrorFile.WriteLine(string.Format("{0}: Report Type {1} currently not supported.", ErrorDate, reportType));
                    break;
            }
        }

        private static void ParseJR1v3(SushiReport sushiReport, TextWriter tw)
        {

            // only do one report for now
            if (sushiReport.CounterReports.Count > 0)
            {
                CounterReport report = sushiReport.CounterReports[0];

                foreach (JournalReportItem reportItem in report.ReportItems)
                {
                    StringBuilder line =
                        new StringBuilder(WrapComma(reportItem.Name) + "," + WrapComma(reportItem.Publisher) + "," +
                                          WrapComma(reportItem.Platform) + "," + reportItem.PrintISSN + "," + reportItem.OnlineISSN);


                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        DateTime start = new DateTime(currMonth.Year, currMonth.Month, 1);
                        DateTime end = new DateTime(currMonth.Year, currMonth.Month, DateTime.DaysInMonth(currMonth.Year, currMonth.Month));

                        CounterMetric metric;

                        bool foundCount = false;
                        if (reportItem.TryGetMetric(start, end, CounterMetricCategory.Requests, out metric))
                        {
                            foreach (var instance in metric.Instances)
                            {
                                // get ft_total only
                                if (!foundCount && instance.Type == CounterMetricType.ft_total)
                                {
                                    line.Append("," + instance.Count);
                                    foundCount = true;
                                }
                            }
                            
                        }

                        if (!foundCount)
                        {
                            line.Append(",");
                        }
                    }


                    // fill YTD with zeros since this can't be calculated
                    line.Append(",0");
                    line.Append(",0");
                    line.Append(",0");

                    tw.WriteLine(line);

                }
            }
        }

        private static void ParseJR1v1(XmlDocument sushiDoc, TextWriter tw)
        {

            // write entries
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(sushiDoc.NameTable);
            xmlnsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlnsManager.AddNamespace("sushi", "http://www.niso.org/schemas/sushi/1_5");
            XmlNodeList entries = sushiDoc.SelectNodes("//sushi:journal", xmlnsManager);

            if (entries != null)
            {
                foreach (XmlNode entry in entries)
                {
                    StringBuilder journal = new StringBuilder(WrapComma(entry.Attributes["name"].Value));
                    journal.Append("," + WrapComma(entry.Attributes["publisher"].Value));
                    journal.Append("," + WrapComma(entry.Attributes["platform"].Value));
                    journal.Append("," + WrapComma(entry.Attributes["print_issn"].Value));
                    journal.Append("," + WrapComma(entry.Attributes["online_issn"].Value));

                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        journal.Append(",");
                        // assume end date matches end of month
                        XmlNode request =
                            entry.SelectSingleNode(
                                string.Format("sushi:requests[@start='{0}' and @type='ft_total']",
                                              currMonth.ToString("yyyy-MM-dd")), xmlnsManager);

                        if (request != null && request.InnerText != string.Empty)
                        {
                            //strip comma from numbers just in case
                            journal.Append(request.InnerText.Replace(",", ""));
                        }
                        else
                        {
                            // since 360 counter does not accept blank cells, add a zero if usage stat is missing
                            journal.Append("0");
                        }
                    }

                    // fill YTD with zeros since this can't be calculated
                    journal.Append(",0");
                    journal.Append(",0");
                    journal.Append(",0");

                    tw.WriteLine(journal);
                }
            }
        }
        private static void ParseDB1v3(XmlDocument sushiDoc, TextWriter tw)
        {
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(sushiDoc.NameTable);

            // Proquest
            xmlnsManager.AddNamespace("c", "http://www.niso.org/schemas/counter");

            XmlNodeList entries = sushiDoc.SelectNodes("//c:ReportItems", xmlnsManager);

            if (entries != null)
            {
                foreach (XmlNode entry in entries)
                {
                    StringBuilder database = new StringBuilder(WrapComma(entry.SelectSingleNode("c:ItemName", xmlnsManager).InnerText));
                    database.Append("," + WrapComma(entry.SelectSingleNode("c:ItemPublisher", xmlnsManager).InnerText));
                    database.Append("," + WrapComma(entry.SelectSingleNode("c:ItemPlatform", xmlnsManager).InnerText));

                    MonthData = new Dictionary<string, string>();
                    XmlNodeList months = entry.SelectNodes("c:ItemPerformance", xmlnsManager);
                    if (months != null)
                    {   
                        foreach (XmlNode month in months)
                        {
                            DateTime startDate;
                            DateTime endDate;
                            DateTime.TryParse(month.SelectSingleNode("c:Period/c:Begin", xmlnsManager).InnerText, out startDate);
                            DateTime.TryParse(month.SelectSingleNode("c:Period/c:End", xmlnsManager).InnerText, out endDate);

                            // check that it's data for only one month (ignore multi-month data)
                            if (startDate.Month == endDate.Month)
                            {
                                // get searches and sessions
                                if (month.SelectSingleNode("c:Instance/c:MetricType", xmlnsManager).InnerText == "Count")
                                {
                                    try
                                    {
                                        MonthData.Add(startDate.ToString("MMM-yy") + month.SelectSingleNode("c:Category", xmlnsManager).InnerText, month.SelectSingleNode("c:Instance/c:Count", xmlnsManager).InnerText);
                                    }

                                    catch (ArgumentException)
                                    {
                                        Console.Out.WriteLine(string.Format("Warning: Ignoring Duplicates for Month: {0}, Category: {1}", startDate.ToString("MMM-yy"), month.SelectSingleNode("c:Category", xmlnsManager).InnerText));
                                    }
                                }
                            }
                        }
                    }

                    StringBuilder searches = new StringBuilder(database + ",Searches run");

                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        if (MonthData.ContainsKey(currMonth.ToString("MMM-yy") + "Searches"))
                        {
                            searches.Append("," + MonthData[currMonth.ToString("MMM-yy") + "Searches"]);
                        }
                        else
                        {
                            searches.Append(",");
                        }
                    }
                    // fill YTD with zeros since this can't be calculated
                    searches.Append(",0");
                    tw.WriteLine(searches);

                    StringBuilder sessions = new StringBuilder(database + ",Sessions");

                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        if (MonthData.ContainsKey(currMonth.ToString("MMM-yy") + "Sessions"))
                        {
                            sessions.Append("," + MonthData[currMonth.ToString("MMM-yy") + "Sessions"]);
                        }
                        else
                        {
                            sessions.Append(",");
                        }
                    }
                    // fill YTD with zeros since this can't be calculated
                    sessions.Append(",0");
                    tw.WriteLine(sessions);
                }
            }
        }

        private static void ParseDB3v3(XmlDocument sushiDoc, TextWriter tw)
        {
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(sushiDoc.NameTable);

            // Proquest
            xmlnsManager.AddNamespace("c", "http://www.niso.org/schemas/counter");

            XmlNodeList entries = sushiDoc.SelectNodes("//c:ReportItems", xmlnsManager);

            if (entries != null)
            {
                foreach (XmlNode entry in entries)
                {
                    StringBuilder database = new StringBuilder(WrapComma(entry.SelectSingleNode("c:ItemName", xmlnsManager).InnerText));
                    database.Append("," + WrapComma(entry.SelectSingleNode("c:ItemPlatform", xmlnsManager).InnerText));

                    MonthData = new Dictionary<string, string>();
                    XmlNodeList months = entry.SelectNodes("c:ItemPerformance", xmlnsManager);
                    if (months != null)
                    {
                        foreach (XmlNode month in months)
                        {
                            DateTime startDate;
                            DateTime endDate;
                            DateTime.TryParse(month.SelectSingleNode("c:Period/c:Begin", xmlnsManager).InnerText, out startDate);
                            DateTime.TryParse(month.SelectSingleNode("c:Period/c:End", xmlnsManager).InnerText, out endDate);

                            // check that it's data for only one month (ignore multi-month data)
                            if (startDate.Month == endDate.Month)
                            {
                                // get searches and sessions
                                if (month.SelectSingleNode("c:Instance/c:MetricType", xmlnsManager).InnerText == "Count")
                                {
                                    try
                                    {
                                        MonthData.Add(startDate.ToString("MMM-yy") + month.SelectSingleNode("c:Category", xmlnsManager).InnerText, month.SelectSingleNode("c:Instance/c:Count", xmlnsManager).InnerText);   
                                    }
                                    catch (ArgumentException)
                                    {
                                        Console.Out.WriteLine(string.Format("Warning: Ignoring Duplicates for Month: {0}, Category: {1}", startDate.ToString("MMM-yy"), month.SelectSingleNode("c:Category", xmlnsManager).InnerText));
                                    }
                                }
                            }
                        }
                    }

                    StringBuilder searches = new StringBuilder(database + ",Searches run");

                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        if (MonthData.ContainsKey(currMonth.ToString("MMM-yy") + "Searches"))
                        {
                            searches.Append("," + MonthData[currMonth.ToString("MMM-yy") + "Searches"]);
                        }
                        else
                        {
                            searches.Append(",");
                        }
                    }
                    // fill YTD with zeros since this can't be calculated
                    searches.Append(",0");
                    tw.WriteLine(searches);

                    StringBuilder sessions = new StringBuilder(database + ",Sessions");

                    for (DateTime currMonth = StartDate; currMonth <= EndDate; currMonth = currMonth.AddMonths(1))
                    {
                        if (MonthData.ContainsKey(currMonth.ToString("MMM-yy") + "Sessions"))
                        {
                            sessions.Append("," + MonthData[currMonth.ToString("MMM-yy") + "Sessions"]);
                        }
                        else
                        {
                            sessions.Append(",");
                        }
                    }
                    // fill YTD with zeros since this can't be calculated
                    sessions.Append(",0");
                    tw.WriteLine(sessions);
                }
            }
        }

        // wrap string in quotes if it contains commas
        private static string WrapComma(string input)
        {
            if (input.Contains(","))
            {
                input = "\"" + input + "\"";
            }
            return input;
        }
    }
}
