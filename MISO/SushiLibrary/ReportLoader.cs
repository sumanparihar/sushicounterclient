using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SushiLibrary
{
    public static class ReportLoader
    {
        public static SushiReport LoadCounterReport(XmlDocument reportXml)
        {
            SushiReport sushiReport = new SushiReport();

            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(reportXml.NameTable);

            xmlnsManager.AddNamespace("c", "http://www.niso.org/schemas/counter");
            xmlnsManager.AddNamespace("s", "http://www.niso.org/schemas/sushi");

            sushiReport.ReportType = 
                (CounterReportType)Enum.Parse(typeof(CounterReportType), reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Name"].Value, true);

            sushiReport.Release = reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Release"].Value;

            XmlNodeList reports = reportXml.SelectNodes("//c:Report", xmlnsManager);

            if (reports != null)
            {
                sushiReport.CounterReports = new List<CounterReport>();

                foreach (XmlNode report in reports)
                {
                    CounterReport counterReport = new CounterReport();
                    counterReport.ID = report.Attributes["ID"].Value;
                    counterReport.Name = report.Attributes["Name"].Value;
                    counterReport.Title = report.Attributes["Title"].Value;
                    counterReport.Version = report.Attributes["Version"].Value;
                    DateTime created;
                    DateTime.TryParse(report.Attributes["Created"].Value, out created);
                    counterReport.Created = created;


                    XmlNodeList reportItems = report.SelectNodes("c:Customer/c:ReportItems", xmlnsManager);

                    if (reportItems != null)
                    {
                        counterReport.CounterReportItems = new List<CounterReportItem>();

                        foreach (XmlNode reportItem in reportItems)
                        {
                            CounterReportItem counterReportItem;

                            switch (sushiReport.ReportType)
                            {
                                case CounterReportType.JR1:
                                case CounterReportType.JR2:
                                case CounterReportType.JR3:
                                case CounterReportType.JR4:
                                case CounterReportType.JR5:
                                    JournalReport journalReport = new JournalReport();
                                    XmlNodeList identifiers = reportItem.SelectNodes("c:ItemIdentifier", xmlnsManager);

                                    if (identifiers != null)
                                    {
                                        foreach (XmlNode identifier in identifiers)
                                        {
                                            string value = identifier.SelectSingleNode("c:Value", xmlnsManager).InnerText;
                                            switch (identifier.SelectSingleNode("c:Type", xmlnsManager).InnerText.ToLower())
                                            {
                                                // see http://www.niso.org/workrooms/sushi/values/#item
                                                case "issn":
                                                    journalReport.PrintISSN = value;
                                                    break;
                                                case "print_issn":
                                                    journalReport.PrintISSN = value;
                                                    break;
                                                case "online_issn":
                                                    journalReport.OnlineISSN = value;
                                                    break;
                                            }
                                        }
                                    }

                                    counterReportItem = journalReport;
                                    break;
                                default:
                                    counterReportItem = new CounterReportItem();
                                    break;
                            }


                            counterReportItem.Name = reportItem.SelectSingleNode("c:ItemName", xmlnsManager).InnerText;
                            counterReportItem.Publisher = reportItem.SelectSingleNode("c:ItemPublisher", xmlnsManager).InnerText;
                            counterReportItem.Platform = reportItem.SelectSingleNode("c:ItemPlatform", xmlnsManager).InnerText;

                            XmlNodeList metrics = reportItem.SelectNodes("c:ItemPerformance", xmlnsManager);

                            if (metrics != null)
                            {
                                counterReportItem.Metrics = new Dictionary<string, CounterMetric>();

                                foreach (XmlNode metric in metrics)
                                {
                                    DateTime start, end;
                                    DateTime.TryParse(metric.SelectSingleNode("c:Period/c:Begin", xmlnsManager).InnerText, out start);
                                    DateTime.TryParse(metric.SelectSingleNode("c:Period/c:End", xmlnsManager).InnerText, out end);

                                    CounterMetric counterMetric;

                                    // merge different instances under the same time period
                                    if (!counterReportItem.Metrics.TryGetValue(start + "|" + end,  out counterMetric))
                                    {
                                        counterMetric = new CounterMetric();
                                    }

                                    counterMetric.Start = start;

                                    counterMetric.End = end;

                                    counterMetric.Category =
                                        (CounterMetricCategory)
                                        Enum.Parse(typeof (CounterMetricCategory),
                                                   metric.SelectSingleNode("c:Category", xmlnsManager).InnerText, true);

                                    XmlNodeList instances = metric.SelectNodes("c:Instance", xmlnsManager);

                                    if (instances != null)
                                    {
                                        counterMetric.Instance = new List<CounterMetricInstance>();
                                        foreach (XmlNode instance in instances)
                                        {
                                            CounterMetricInstance metricInstance= new CounterMetricInstance();
                                            metricInstance.Type =
                                                (CounterMetricType)
                                                Enum.Parse(typeof (CounterMetricType),
                                                           instance.SelectSingleNode("c:MetricType",
                                                                                   xmlnsManager).InnerText, true);

                                            int count;
                                            Int32.TryParse(
                                                instance.SelectSingleNode("c:Count", xmlnsManager).InnerText,
                                                out count);
                                            metricInstance.Count = count;

                                            counterMetric.Instance.Add(metricInstance);
                                        }

                                    }

                                    counterReportItem.Metrics[start + "|" + end] = counterMetric;

                                }
                            }
                           
                            counterReport.CounterReportItems.Add(counterReportItem);

                        }
                    }

                    sushiReport.CounterReports.Add(counterReport);
                }
            }


            return sushiReport;
        }

    }
}
