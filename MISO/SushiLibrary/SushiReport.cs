using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SushiLibrary
{
    public class SushiReport
    {

        public CounterReportType ReportType { get; set; }
        public string Release { get; set; }

        public List<CounterReport> CounterReports { get; set; }
    }
}
