using OpenCvSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
public class Image {
    public static class Is {//主としてif文で使う               変数名に焦点
        public const int Color = 3;
        public const int GrayScale = 1;
        public const bool DESCENDING_ORDER = true;//Value is meaningless
        public const bool ASCENDING_ORDER = false;//Value is meaningless
    }
    public class Const {//配列の宣言で使うことが多い 値に焦点
        public const int Tone8Bit = 256;
        //public const int Neighborhood8 = 9;
        //public const int Neighborhood4 = 5;
    }
    public static int GetHistgramR(in string f, ref byte[] Histgram) {//ここでグレーかカラーか判定
        int Channel = Is.GrayScale;//1:gray,3:bgr color
        System.IO.MemoryStream data = new System.IO.MemoryStream(File.ReadAllBytes(f));
        WriteableBitmap wbmp = new WriteableBitmap(BitmapFrame.Create(data));
        data.Close();
        FormatConvertedBitmap bitmap = new FormatConvertedBitmap(wbmp, PixelFormats.Pbgra32, null, 0);// BitmapImageのPixelFormatをPbgra32に変換する
        byte[] originalPixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];// 画像の大きさに従った配列を作る
        bitmap.CopyPixels(originalPixels, (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8, 0); // BitmapSourceから配列にコピー

        for (int i = 0; i < originalPixels.Length; i += 4) {
            if (Channel == Is.Color || originalPixels[i] != originalPixels[i + 1] || originalPixels[i + 2] != originalPixels[i]) {//Color images are not executed.
                Channel = Is.Color;
                Histgram[(byte)((originalPixels[i] + originalPixels[i + 1] + originalPixels[i + 2] + 0.5) / 3)]++;//四捨五入
            } else
                Histgram[originalPixels[i]]++;
        }
        System.GC.Collect();
        return Channel;
    }
    private static byte CheckRange2Byte(double ByteValue) {
        return (byte)(ByteValue > 255 ? 255 : ByteValue < 0 ? 0 : ByteValue);
    }
    public static unsafe void LinearStretch(Mat p_img, int HistgramMax, int HistgramMin) {//階調値の線形変換 グレイスケールのみ
        double magnification = 255.99 / (HistgramMax - HistgramMin);//255.99ないと255が254になる
        byte* p = p_img.DataPointer;//byte* p = (byte*)p_img.ImageData;
        for (int y = 0; y < p_img.Width * p_img.Height; ++y)
            p[y] = Image.CheckRange2Byte(magnification * (p[y] - HistgramMin));
    }

    private static byte GetBucketMedianAscendingOrder(int[] Bucket, int Median) {
        byte YIndex = 0;//256 探索範囲の最小値を探す　
        int ScanHalf = 0;
        while ((ScanHalf += Bucket[YIndex++]) < Median) ;//Underflow
        return --YIndex;
    }/* */
    private static byte GetBucketMedianDescendingOrder(int[] Bucket, int Median) {
        byte YIndex = 0;//中央値を下(黒)からを探す　
        int ScanHalf = 0;
        while ((ScanHalf += Bucket[--YIndex]) < Median) ;//Underflow
        return YIndex;
    }
    private static unsafe bool SelectAscendingDescendingOrder(Mat src_img) {
        byte* src = src_img.DataPointer;//byte* src = (byte*)src_img.ImageData;
        return src[0] + src[src_img.Width * src_img.Height - (src_img.Width - src_img.Width) - 1] + src[src_img.Width - 1] + src[src_img.Width * src_img.Height - src_img.Width - 1] > 511 ? Is.DESCENDING_ORDER : Is.ASCENDING_ORDER;
    }
    delegate byte SelectBucketMedian(int[] Bucket, int Median);
    public static unsafe bool FastestMedian(Mat src_img, Mat dst_img, int n) {
        dst_img = src_img.Clone();//Cv.Copy(src_img, dst_img);
        if ((n & 1) == 0) return false;//偶数はさいなら 元のをコピー
        int MaskSize = n >> 1;//
        SelectBucketMedian BucketMedian = GetBucketMedianAscendingOrder;
        if (SelectAscendingDescendingOrder(src_img) == Is.DESCENDING_ORDER)
            BucketMedian = GetBucketMedianDescendingOrder;
        byte* dst = dst_img.DataPointer;//byte* dst = (byte*)dst_img.ImageData;
        dst += MaskSize * (src_img.Width) + MaskSize;
        for (int y = MaskSize; y < src_img.Height - MaskSize; ++y, dst += src_img.Width) {
            int[] Bucket = new int[Const.Tone8Bit];//256tone It is cleared each time
            for (int x = 0; x < n; ++x) {
                byte* src = src_img.DataPointer;//byte* src = (byte*)src_img.ImageData
                src += (y - MaskSize) * src_img.Width + x;
                for (int yy = 0; yy < n; ++yy, src += src_img.Width)
                    ++Bucket[*src];
            }
            *dst = BucketMedian(Bucket, ((n * n) >> 1));

            for (int x = 0; x < src_img.Width - n; ++x) {
                byte* src = src_img.DataPointer;//byte* src = (byte*)src_img.ImageData
                src += (y - MaskSize) * src_img.Width + x;
                for (int yy = 0; yy < n; ++yy, src += src_img.Width) {
                    --Bucket[*src];
                    ++Bucket[*(src + n)];
                }
                *(dst + x + 1) = BucketMedian(Bucket, ((n * n) >> 1));
            }
        }
        return true;
    }
}
