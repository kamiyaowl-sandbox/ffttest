using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FFTTest {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        private Chart freqChart;
        private Chart timeChart;
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            timeChart = (Chart)timeChartHost.Child;
            freqChart = (Chart)frequencyChartHost.Child;

            // ChartArea追加
            timeChart.ChartAreas.Add("ChartArea1");
            timeChart.ChartAreas[0].AxisX.Title = "Time";
            timeChart.ChartAreas[0].AxisY.Title = "Voltage";
            timeChart.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
            timeChart.ChartAreas[0].AxisY.MajorGrid.Enabled = true;
            freqChart.ChartAreas.Add("ChartArea1");
            freqChart.ChartAreas[0].AxisX.Title = "Freq";
            freqChart.ChartAreas[0].AxisY.Title = "Power";
            freqChart.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
            freqChart.ChartAreas[0].AxisY.MajorGrid.Enabled = true;

        }

        private void startFFTButton_Click(object sender, RoutedEventArgs e) {
            double sampleFreq = 0;
            WaveGenerator waveGen = null;
            IEnumerable<double> srcDatas = null;
            int srcSampleN;
            Complex[] srcArr;
            Complex[] dstArr;
            //サンプリング周波数
            srcSampleN = int.Parse(sampleNCombo.Text);
            switch (sampleFreqCombo.SelectedIndex) {
                case 0:
                    sampleFreq = 44.1e3;
                    break;
                case 1:
                    sampleFreq = 96e3;
                    break;
                case 2:
                    sampleFreq = 192e3;
                    break;
            }

            //入力データ
            waveGen = new WaveGenerator((int)sampleFreq);
            double srcVm = 2;
            double srcFreq = double.Parse(srcFreqText.Text);
            switch (srcCombo.SelectedIndex) {
                case 0:
                    srcDatas = waveGen.GenerateWave(WaveGenerator.SinFunc(srcVm, 0, srcFreq));
                    break;
                case 1:
                    srcDatas = waveGen.GenerateWave(WaveGenerator.SquareFunc(srcVm, 0, srcFreq));
                    break;
                case 2:
                    srcDatas = waveGen.GenerateWave(WaveGenerator.SawFunc(srcVm, 0, srcFreq));
                    break;
            }
            srcArr = srcDatas.Take(srcSampleN).Select(x => new Complex(x, 0)).ToArray();
            dstArr = new Complex[srcSampleN];
            //一旦表示
            timeChart.Series.Clear();
            timeChart.AddSeries("src", sampleFreq, srcDatas.Take(srcSampleN));
            //FFT開始
            var startTime = Environment.TickCount;
            fft(srcSampleN, srcArr, ref dstArr);
            var elapsedTime = Environment.TickCount - startTime;
            timeText.Content = $"{elapsedTime}[ms]";

            freqChart.Series.Clear();
            freqChart.AddSeries("fft", dstArr.Select(x => x.Magnitude).ToArray());

        }

        private static void bitReverseArrTest() {
            for (int i = 0; i < 8; ++i) {
                int max = (int)Math.Pow(2, i);
                var table = generateBitReverseArr(max);
                Debug.WriteLine($"MAX:{max}");
                foreach (var data in table.Select((x, y) => new { Index = y, Reversed = x })) {
                    Debug.WriteLine($"{i}\t{data.Index:X}\t{data.Reversed:X}");
                }
            }
        }

        /// <summary>
        /// ビット反転テーブルを生成します
        /// </summary>
        /// <param name="max">ビット幅</param>
        /// <returns></returns>
        private static IEnumerable<int> generateBitReverseArr(int width) {
            int max = 0x1 << width;
            for (int j = 0; j < max; ++j) {
                int data = 0x0;
                for (int i = 0; i < width; ++i) {
                    data |= (((j >> i) & 0x1) << (width - 1 - i));
                }
                yield return data;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleN"></param>
        /// <param name="srcArr">データバッファ、データの並び替えは不要</param>
        /// <param name="dstArr"></param>
        private static void fft(int sampleN, Complex[] srcArr, ref Complex[] dstArr) {
            srcArr.CopyTo(dstArr, 0);

            int stageN = (int)Math.Log(sampleN, 2);//定数
            /* 回転子の事前計算 */
            var wMax = sampleN / 2;
            var wTable = Enumerable.Range(0, wMax).Select(i => Complex.Exp(-i * 2 * Math.PI / sampleN)).ToArray();
            /* FFt本体の計算、複数ステージのバタフライ演算をする */
            for (int stage = 0; stage < stageN; ++stage) {
                int indexN = sampleN >> (stage + 1);//sampleN -> sampleN/2 -> sampleN/4 ... 1
                int subIndexN = 0x1 << (stage + 1);//2 -> 4 -> 8-> ... sampleN
                int bitLength = stageN - stage;//ビット反転アドレッシングの幅
                var addressingArr = generateBitReverseArr(bitLength).ToArray();//アドレッシング（多分ソフトのみ)

                Debug.WriteLine($"STAGE{stage} --bit-length[{bitLength}]--> Index[{indexN}][{subIndexN}]");
                //0 ~ sampleN / 2まで2個ずつ処理する
                for (int i = 0; i < sampleN / 2; ++i) {
                    //ビット反転前のインデックス
                    int index1 = (i >> stage) << 1;
                    int index2 = index1 + 1;
                    int subIndex = i & ~(0xffff << stage);
                    //ビット反転後のインデックス
                    int reverseIndex1 = addressingArr[index1];
                    int reverseIndex2 = addressingArr[index2];
                    //実データへのアドレスは {index,subIndex}
                    int addr1 = (reverseIndex1 << stage) + subIndex;//元の配列の位置
                    int addr2 = (reverseIndex2 << stage) + subIndex;//元の配列の位置
                    //回転子決定
                    int wIndex = (((subIndex) & ~(0xffff << stage)) << (stageN - stage - 1));
                    Debug.WriteLine($"i:{i}\tindex[{index1}, {index2}]\treverseIndex[{reverseIndex1}, {reverseIndex2}]\tsubIndex:{subIndex}\taddr[{addr1}, {addr2}]\twIndex:{wIndex}");

                    //計算
                    var srcData1 = dstArr[addr1];
                    var srcData2 = dstArr[addr2];
                    var w = wTable[wIndex];
                    var multiplyData = w * srcData2;
                    var dstData1 = srcData1 + multiplyData;
                    var dstData2 = srcData1 - multiplyData;
                    dstArr[addr1] = dstData1;
                    dstArr[addr2] = dstData2;
                }


            }
        }
    }
}
