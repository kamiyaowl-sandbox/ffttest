using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace FFTTest {
    public static class ChartExtension {
        public static void AddSeries(this Chart c, string name, IEnumerable<double> data) {
            // Seriesの作成と値の追加
            var series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            series.LegendText = name;

            foreach (var d in data) {
                series.Points.Add(d);
            }
            c.Series.Add(series);
        }
        public static void AddSeries(this Chart c, string name, IEnumerable<DataPoint> data) {
            // Seriesの作成と値の追加
            var series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            series.LegendText = name;
            foreach (var d in data) {
                series.Points.Add(d);
            }
            c.Series.Add(series);
        }
        public static void AddSeries(this Chart c, string name, double sampleFreq, IEnumerable<double> data) {
            // Seriesの作成と値の追加
            var series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            series.LegendText = name;
            double t = 0;
            var period = 1.0 / sampleFreq;
            foreach (var d in data) {
                series.Points.AddXY(t, d);
                t += period;
            }
            c.Series.Add(series);
        }
    }

}
