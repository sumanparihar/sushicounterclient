using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary
{
    public class CounterReportItem
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Platform { get; set; }

        public Dictionary<string, CounterMetric> Metrics { get; set; }
    }
}
