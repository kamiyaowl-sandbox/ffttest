using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTest {
    /// <summary>
    /// 符号付き固定小数点を表現します
    /// </summary>
    class SignedFixedPoint {
        /// <summary>
        /// 符号ビット
        /// </summary>
        public bool Sign { get; private set; }
        /// <summary>
        /// 整数部の生の値です
        /// </summary>
        public UInt32 IntegersRaw { get; private set; }
        /// <summary>
        /// 小数部の生の値です。右詰めで保持しています
        /// </summary>
        public UInt32 DecimalsRaw { get; private set; }
        /// <summary>
        /// 整数部のデータ幅です
        /// </summary>
        public int IntegersWidth { get; private set; }
        /// <summary>
        /// 小数部のデータ幅です
        /// </summary>
        public int DecimalsWidth { get; private set; }
        /// <summary>
        /// 全体のデータ幅です
        /// </summary>
        public int Width => 1 + IntegersWidth + DecimalsWidth;
        /// <summary>
        /// データ幅でマスクされた整数部を返します
        /// </summary>
        public UInt32 MaskIntegers => IntegersRaw & ~(0xffffffff << IntegersWidth);
        /// <summary>
        /// データ幅でマスクされた小数部を返します。右詰めで保持しています
        /// </summary>
        public UInt32 MaskDecimals => DecimalsRaw & ~(0xffffffff << DecimalsWidth);
        /// <summary>
        /// 符号付きの整数部の値を返します
        /// </summary>
        public Int32 SignedIntegers => (Int32)(Sign ? -MaskIntegers : MaskIntegers);
        /// <summary>
        /// 符号付きの小数部の値を返します。右詰めのままなので値は2^DecimalsWidthする前の状態です
        /// 正しい値はDoubleDecimalsから取得できます
        /// </summary>
        public Int32 SignedDecimals => (Int32)(Sign ? -MaskDecimals : MaskDecimals);
        /// <summary>
        /// 整数部を実数で返します
        /// </summary>
        public double DoubleIntegers => (double)MaskIntegers;
        /// <summary>
        /// 小数部を実数で返します
        /// </summary>
        public double DoubleDecimals => (double)MaskDecimals / Math.Pow(2, DecimalsWidth);
        /// <summary>
        /// 値の取り出し、設定ができます
        /// </summary>
        public double Double {
            get { return (Sign ? -1 : 1) * DoubleIntegers + DoubleDecimals; }
            set {
                this.Sign = value < 0;
                this.IntegersRaw = (UInt32)Math.Abs(value) & ~(0xffffffff << IntegersWidth);
                this.DecimalsRaw = (UInt32)(Math.Abs(value) * Math.Pow(2, DecimalsWidth)) & ~(0xffffffff << DecimalsWidth);
            }
        }
        /// <summary>
        /// VerilogHDLで表現されるフォーマットで返します
        /// </summary>
        public string VerilogFormat => $"{Width}'b{(Sign ? '1' : '0')}_{Convert.ToString(MaskIntegers, 2).PadLeft(IntegersWidth, '0')}_{Convert.ToString(MaskDecimals, 2).PadLeft(DecimalsWidth, '0')}";

        public override string ToString() => $"{VerilogFormat}({Double})";


        public SignedFixedPoint(int integersWidth, int decimalsWidth) {
            this.IntegersWidth = integersWidth;
            this.DecimalsWidth = decimalsWidth;
        }

        public static SignedFixedPoint operator +(SignedFixedPoint fp1, SignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            var d = (UInt32)(fp1.MaskDecimals + fp2.MaskDecimals);//TODO:マイナスの処理
            var i = fp1.SignedIntegers + fp2.SignedIntegers + (d >> fp1.DecimalsWidth);
            return new SignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                Sign = d < 0,
                DecimalsRaw = d,
                IntegersRaw = (UInt32)i,
            };
        }
    }
}
