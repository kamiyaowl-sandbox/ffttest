using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTest {
    public abstract class FixedPoint {
        /// <summary>
        /// 正負を返します
        /// </summary>
        public abstract bool IsSigned { get; }
        /// <summary>
        /// 整数部の生の値です
        /// </summary>
        public UInt32 IntegersRaw { get; protected set; }
        /// <summary>
        /// 小数部の生の値です。右詰めで保持しています
        /// </summary>
        public UInt32 DecimalsRaw { get; protected set; }
        /// <summary>
        /// 整数部のデータ幅です
        /// </summary>
        public int IntegersWidth { get; protected set; }
        /// <summary>
        /// 小数部のデータ幅です
        /// </summary>
        public int DecimalsWidth { get; protected set; }
        /// <summary>
        /// 全体のデータ幅です
        /// </summary>
        public int Width => IntegersWidth + DecimalsWidth;
        /// <summary>
        /// データ幅でマスクされた整数部を返します
        /// </summary>
        public UInt32 MaskIntegers => IntegersRaw & ~(0xffffffff << IntegersWidth);
        /// <summary>
        /// データ幅でマスクされた小数部を返します。右詰めで保持しています
        /// </summary>
        public UInt32 MaskDecimals => DecimalsRaw & ~(0xffffffff << DecimalsWidth);

        /// <summary>
        /// 整数部を実数で返します
        /// </summary>
        protected double DoubleIntegers => (double)MaskIntegers;
        /// <summary>
        /// 小数部を実数で返します
        /// </summary>
        protected double DoubleDecimals => (double)MaskDecimals / Math.Pow(2, DecimalsWidth);
        /// <summary>
        /// 値の取り出し、設定ができます
        /// </summary>
        public abstract double DoubleValue { get; set; }

        /// <summary>
        /// 生のビット列で返します
        /// </summary>
        public UInt64 RawData {
            get { return (((UInt64)MaskIntegers << DecimalsWidth) | ((UInt64)MaskDecimals)); }
            set {
                IntegersRaw = (UInt32)(value >> DecimalsWidth) & ~(0xffffffff << IntegersWidth);
                DecimalsRaw = (UInt32)(value & ~(0xffffffff << DecimalsWidth));
            }
        }
        /// <summary>
        /// 正負を反転した値を返します
        /// </summary>
        public SignedFixedPoint TwoComplementary {
            get {
                var comp = ((UInt64)(~RawData) + 0x1) & ~((UInt64)0xffffffffffffffff << Width);
                return new SignedFixedPoint(IntegersWidth, DecimalsWidth) {
                    RawData = comp,
                };
            }
        }
        /// <summary>
        /// VerilogHDLで表現されるフォーマットで返します
        /// </summary>
        public string VerilogFormat => $"{Width}'b{Convert.ToString(MaskIntegers, 2).PadLeft(IntegersWidth, '0')}_{Convert.ToString(MaskDecimals, 2).PadLeft(DecimalsWidth, '0')}";
        public override string ToString() => $"{VerilogFormat}({DoubleValue})";

        public FixedPoint(int integersWidth, int decimalsWidth) {
            Debug.Assert(0 < integersWidth && integersWidth < 32);
            Debug.Assert(0 < decimalsWidth && decimalsWidth < 32);

            this.IntegersWidth = integersWidth;
            this.DecimalsWidth = decimalsWidth;
        }

        public static explicit operator double(FixedPoint fp) => fp.DoubleValue;
        public static explicit operator int(FixedPoint fp) => (int)fp.DoubleValue;
    }

    /// <summary>
    /// 2の補数表現を用いた符号付き固定小数点を表現します
    /// </summary>
    public class SignedFixedPoint : FixedPoint {
        public override bool IsSigned => (IntegersRaw >> (IntegersWidth - 1)) != 0x0;
        public override double DoubleValue {
            get {
                if (IsSigned) {
                    //元の数に戻してから符号をかける
                    var comp = TwoComplementary;
                    return -1 * (comp.DoubleIntegers + comp.DoubleDecimals);
                } else {
                    return DoubleIntegers + DoubleDecimals;
                }
            }
            set {
                if (value < 0) {
                    //反転させたビットを設定
                    var fp = new SignedFixedPoint(IntegersWidth, DecimalsWidth) { DoubleValue = -value };
                    var comp = fp.TwoComplementary;
                    this.RawData = comp.RawData;
                } else {
                    this.IntegersRaw = (UInt32)Math.Abs(value) & ~(0xffffffff << IntegersWidth);
                    this.DecimalsRaw = (UInt32)(Math.Abs(value) * Math.Pow(2, DecimalsWidth)) & ~(0xffffffff << DecimalsWidth);
                }
            }
        }
        public SignedFixedPoint(int integersWidth, int decimalsWidth) : base(integersWidth, decimalsWidth) {
        }

        public static SignedFixedPoint operator +(SignedFixedPoint fp1, SignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            return new SignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                RawData = fp1.RawData + fp2.RawData,
            };
        }
        public static SignedFixedPoint operator -(SignedFixedPoint fp1, SignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            return new SignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                RawData = fp1.RawData + fp2.TwoComplementary.RawData,
            };
        }
        /// <summary>
        /// TODO:部分積の足し合わせで実装する
        /// </summary>
        /// <param name="fp1"></param>
        /// <param name="fp2"></param>
        /// <returns></returns>
        public static SignedFixedPoint operator *(SignedFixedPoint fp1, SignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            return new SignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                DoubleValue = fp1.DoubleValue * fp2.DoubleValue,
            };
        }
    }

    /// <summary>
    /// 符号無し固定小数点を表現します
    /// </summary>
    public class UnsignedFixedPoint : FixedPoint {
        public override bool IsSigned => false;
        public override double DoubleValue {
            get { return DoubleIntegers + DoubleDecimals; }
            set {
                Debug.Assert(value >= 0);
                this.IntegersRaw = (UInt32)Math.Abs(value) & ~(0xffffffff << IntegersWidth);
                this.DecimalsRaw = (UInt32)(Math.Abs(value) * Math.Pow(2, DecimalsWidth)) & ~(0xffffffff << DecimalsWidth);
            }
        }
        public UnsignedFixedPoint(int integersWidth, int decimalsWidth) : base(integersWidth, decimalsWidth) {
        }

        public static UnsignedFixedPoint operator +(UnsignedFixedPoint fp1, UnsignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            return new UnsignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                RawData = fp1.RawData + fp2.RawData,
            };
        }

        public static UnsignedFixedPoint operator -(UnsignedFixedPoint fp1, UnsignedFixedPoint fp2) {
            Debug.Assert(fp1.DecimalsWidth == fp2.DecimalsWidth && fp1.IntegersWidth == fp2.IntegersWidth);
            Debug.Assert(fp1.DoubleValue > fp2.DoubleValue);//符号無しなのでfp2の絶対値ががfp1より大きいと結果がおかしくなります

            return new UnsignedFixedPoint(fp1.IntegersWidth, fp1.DecimalsWidth) {
                RawData = fp1.RawData + fp2.TwoComplementary.RawData,
            };
        }
    }
    public static class FixedPointExtension {
        public static string ToBinaryString(this UInt32 src, int width) => Convert.ToString(src, 2).PadLeft(width, '0');
    }
}
