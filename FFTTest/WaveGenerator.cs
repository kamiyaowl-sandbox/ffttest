using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTest {
    public class WaveGenerator {
        public double SampleRate { get; private set; }
        public double SamplePeriod { get; private set; }

        public WaveGenerator(double sampleRate) {
            SampleRate = sampleRate;
            SamplePeriod = 1.0 / SampleRate;
        }
        /// <summary>
        /// 関数から連続データを生成します
        /// </summary>
        /// <param name="f"></param>
        /// <param name="sampleRate"></param>
        /// <returns></returns>
        public IEnumerable<double> GenerateWave(Func<double, double> f) {
            for (double t = 0; ; t += SamplePeriod) {
                yield return f(t);
            }
        }

        public static Func<double, double> SinFunc(double vMax, double vOffset, double freq) {
            return t => vMax * Math.Sin(t.ToRadian() * freq) + vOffset;
        }
        public static Func<double, double> SquareFunc(double vMax, double vOffset, double freq) {
            var period = 1.0 / freq;
            var halfPeriod = period / 2;
            return t => ((t % period > halfPeriod) ? vMax : -vMax) + vOffset;
        }
        public static Func<double, double> SawFunc(double vMax, double vOffset, double freq) {
            var period = 1.0 / freq;
            return t => ((t % period / period) * 2 * vMax) + vOffset - vMax;
        }

    }
    public static class WaveGeneratorExtension {
        public static double ToRadian(this double t) => 2 * Math.PI * t;

    }
}
