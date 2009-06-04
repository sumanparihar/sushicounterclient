using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary
{
    public class CounterMetricInstance
    {
        public CounterMetricType Type { get; set; }
        public int Count { get; set; }
    }

    public enum CounterMetricType
    {
        ft_ps,
        ft_pdf,
        ft_html,
        ft_total,
        Toc,
        Abstract,
        reference,
        data_set,
        audio,
        video,
        image,
        podcast,
        search_reg,
        search_fed,
        session_reg,
        session_fed,
        count,
        other
    }
}
