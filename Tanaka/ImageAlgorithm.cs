using System.Threading.Tasks;//setparalle
using System.Linq;//enum
using System.Collections.Generic;//enum
using Tanaka;
using OpenCvSharp;
using System;
using System.IO;

public class ImageAlgorithm {
    static class Cleanliness {
        public const int Clean = 15;//小説Text
        public const int Dirty = 25;//図表マンガ 25
    }
    static class Var {//配列の宣言で使うことが多い 不変普遍定数ではない　ユーザが勝手に変えろ
        public const int MaxMarginSize = 4;//実際は＋1
    }
    private static void ShowImage(string WindowName, Mat src) {
        Cv2.ImShow(WindowName, src);
        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }
    public class ImageRect {//画像固有の値だからstaicではない
        public int YLow { get; set; }
        public int XLow { get; set; }
        public int YHigh { get; set; }
        public int XHigh { get; set; }
        public int Height { get; set; }//YHigh-YLow
        public int Width { get; set; }//XHigh-XLow
    }
    public class Threshold {//画像固有の値だからstaicではない
        public byte Concentration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int WidthTimes { get; } = 3;
        public int HeightTimes { get; } = 3;
    }
    private unsafe static int GetMaxSaturation(Mat HSVImage) {
        int MaxSaturation = 0;
        byte* p = HSVImage.DataPointer;
        for (int yx = 1; yx < HSVImage.Height * HSVImage.Width * 3; yx += 3) {//HSVのSを取得したいのでyxの初期値が1，Hなら0，Vなら2
            MaxSaturation = MaxSaturation > p[yx] ? MaxSaturation : p[yx];
        }
        return MaxSaturation;
    }
    public static void ReduceImages(MainWindow ConfMainWindow, string NewPath) {
        if ((bool)ConfMainWindow.To8bitGray.IsChecked) //24bitGrayTo8bitGray やらない選択肢があるから，やらない場合は24bitGrayが存在してしまう，強制化はできない．
            ImageAlgorithm.ColorOrGray(NewPath);
        if ((bool)ConfMainWindow.MarginRemove.IsChecked)
            ImageAlgorithm.RemoveMarginEntry(ConfMainWindow, NewPath);//該当ファイルのあるフォルダの奴はすべて実行される別フォルダに単体コピーが理想*/
        if ((bool)ConfMainWindow.PNGout.IsChecked)
            ImageAlgorithm.ExecutePNGout(ConfMainWindow, in NewPath);
        ImageAlgorithm.CarmineCliAuto(in NewPath);
    }
    private static void ColorOrGray(string PathName) {
        IEnumerable<string> JPGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
        JPGColorOrGray(JPGFiles);
        IEnumerable<string> JPEGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.jpeg", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
        JPGColorOrGray(JPEGFiles);
        IEnumerable<string> PngFiles = System.IO.Directory.EnumerateFiles(PathName, "*.png", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
        PNGColorOrGray(PngFiles);
        IEnumerable<string> PNGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.PNG", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
        PNGColorOrGray(PNGFiles);

    }
    private static void JPGColorOrGray(IEnumerable<string> JPGFiles) {
        if (JPGFiles.Any())
            Parallel.ForEach(JPGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {
                if (Cv2.ImRead(f, ImreadModes.Unchanged).Channels() != 1) {
                    Mat HSVImage = new Mat(); ;
                    Cv2.CvtColor(Cv2.ImRead(f, ImreadModes.Color), HSVImage, ColorConversionCodes.BGR2HSV);// 画像を，HSV色空間に変換
                    if (GetMaxSaturation(HSVImage) < 180) {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                            FileName = "jpegtran.exe",//jpegtran -grayscale -outfile Z:\bin\22\5.5.jpg Z:\bin\22\5.jpg
                            Arguments = "-grayscale -progressive -outfile \"" + f + "\" \"" + f + "\"",
                            UseShellExecute = false,
                            CreateNoWindow = true    // コンソール・ウィンドウを開かない
                        }).WaitForExit();    // プロセスの終了を待つ
                    }
                }
            });
    }
    private static void PNGColorOrGray(IEnumerable<string> PNGFiles) {
        if (PNGFiles.Any())
            Parallel.ForEach(PNGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {
                if (Cv2.ImRead(f, ImreadModes.Unchanged).Channels() != 1) {
                    Mat HSVImage = new Mat();
                    Cv2.CvtColor(Cv2.ImRead(f, ImreadModes.Color), HSVImage, ColorConversionCodes.BGR2HSV);// 画像を，HSV色空間に変換します．//ShowImage(nameof(YUVImage), YUVImage);
                    if (GetMaxSaturation(HSVImage) < 180)
                        Cv2.ImWrite(f, Cv2.ImRead(f, ImreadModes.Grayscale), new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
                }
            });
    }
    private static void FixNoiseInPNG(bool StandardModeIsChecked, bool StrongestModeIsChecked, string f) {
        byte[] OriginHistgram = new byte[Image.Const.Tone8Bit];
        if (Image.GetHistgramR(in f, ref OriginHistgram) == Image.Is.Color) {//カラーでドット埋めは無理
        } else if (StandardModeIsChecked) {//StandardMode 画像改変は行わない
        } else if (StrongestModeIsChecked) {//StrongestMode 画像改変あり，改変後に余白削除
            FixPixelMissing(in f);//ピクセル欠けを修正
            NoiseRemoveBlockTwoArea(in f, OriginHistgram.Max());//小さいゴミ黒削除
            NoiseRemoveWhite(in f, OriginHistgram.Min());//小さいゴミ白削除
            NoiseRemoveBlockTwoArea(in f, OriginHistgram.Max());//小さいゴミ黒削除
            NoiseRemoveWhite(in f, OriginHistgram.Min());//小さいゴミ白削除
        } else {//StrongMode 空白行を削除してから画像改変
            UselessXColumSpacingDeletion(in f);//空白列削除
            UselessYRowSpacingDeletion(in f);//空白行削除
            FixPixelMissing(in f);//ピクセル欠けを修正
            NoiseRemoveBlockTwoArea(in f, OriginHistgram.Max());//小さいゴミ黒削除
            NoiseRemoveWhite(in f, OriginHistgram.Min());//小さいゴミ白削除
        }
    }
    private static bool CutPNGJPGCommon(bool StandardModeIsChecked, bool StrongestModeIsChecked, in string f, Mat InputGrayImage, byte[] Histgram, out int Channel, ImageRect NewImageRect, TextWriter writerSync) {
        Mat LaplacianImage = new Mat();
        MedianLaplacianMedian(StandardModeIsChecked, StrongestModeIsChecked, InputGrayImage, LaplacianImage);//MedianLaplacianMedianをかけて画像平滑化
        Channel = Image.GetHistgramR(in f, ref Histgram);
        if (!GetNewImageSize(LaplacianImage, new Threshold { Concentration = GetConcentrationThreshold(in Histgram, StandardModeIsChecked) }, NewImageRect)) {
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }//勾配が重要？
        LaplacianImage.Dispose();
        writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + "), (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Width + "," + NewImageRect.Height + ")" + ",Min=" + Histgram.Min() + ",Max=" + Histgram.Max());
        return true;
    }
    private static bool CutPNGMarginMain(bool StandardModeIsChecked, bool StrongestModeIsChecked, in string f, TextWriter writerSync) {
        FixNoiseInPNG(StandardModeIsChecked, StrongestModeIsChecked, f);
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);
        byte[] Histgram = new byte[Image.Const.Tone8Bit];
        ImageRect NewImageRect = new ImageRect();
        /*if(!CutPNGJPGCommon(StandardModeIsChecked, StrongestModeIsChecked, in f, InputGrayImage, Histgram, out int Channel, NewImageRect, writerSync))
            return false;/*- */
        Mat LaplacianImage = new Mat();
        MedianLaplacianMedian(StandardModeIsChecked, StrongestModeIsChecked, InputGrayImage, LaplacianImage);//MedianLaplacianMedianをかけて画像平滑化
        int Channel = Image.GetHistgramR(in f, ref Histgram);
        if (!GetNewImageSize(LaplacianImage, new Threshold { Concentration = GetConcentrationThreshold(in Histgram, StandardModeIsChecked) }, NewImageRect)) {
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }//勾配が重要？
        LaplacianImage.Dispose();
        writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + "), (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Width + "," + NewImageRect.Height + ")" + ",Min=" + Histgram.Min() + ",Max=" + Histgram.Max());/*- */
        Mat OutputCutImage;
        if (Channel == Image.Is.GrayScale) {
            OutputCutImage = InputGrayImage.Clone(new OpenCvSharp.Rect(NewImageRect.XLow, NewImageRect.YLow, NewImageRect.Width, NewImageRect.Height));
            InputGrayImage.Dispose();
            Image.LinearStretch(OutputCutImage, Histgram.Max(), Histgram.Min());// 階調値変換
        } else {//Is.Color
            InputGrayImage.Dispose();
            Mat InputColorImage = Cv2.ImRead(f, ImreadModes.Color);
            OutputCutImage = InputColorImage.Clone(new OpenCvSharp.Rect(NewImageRect.XLow, NewImageRect.YLow, NewImageRect.Width, NewImageRect.Height));
            InputColorImage.Dispose();
        }
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        OutputCutImage.Dispose();
        return true;
    }
    private static bool CutJPGMarginMain(bool StandardModeIsChecked, bool StrongestModeIsChecked, in string f, TextWriter writerSync) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat LaplacianImage = new Mat();
        ImageAlgorithm.MedianLaplacianMedian(StandardModeIsChecked, StrongestModeIsChecked, InputGrayImage, LaplacianImage);
        byte[] Histgram = new byte[Image.Const.Tone8Bit];
        _ = Image.GetHistgramR(in f, ref Histgram);
        ImageRect NewImageRect = new ImageRect();
        if (!GetNewImageSize(LaplacianImage, new Threshold { Concentration = GetConcentrationThreshold(in Histgram, StandardModeIsChecked) }, NewImageRect)) {
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }
        LaplacianImage.Dispose();
        writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + "), (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Width + "," + NewImageRect.Height + ")" + ",Min=" + Histgram.Min() + ",Max=" + Histgram.Max());
        //jpegtran.exe -crop 808x1208+0+63 -outfile Z:\bin\22\6.jpg Z:\bin\22\6.jpg
        InputGrayImage.Dispose();
        string Arguments = "-crop " + NewImageRect.Width + "x" + NewImageRect.Height + "+" + NewImageRect.XLow + "+" + NewImageRect.YLow + " -progressive -outfile \"" + f + "\" \"" + f + "\"";
        StandardAlgorithm.ExecuteAnotherApp("jpegtran.exe", in Arguments, false, true);
        return true;
    }
    private static void RemoveMarginEntry(MainWindow ConfMainWindow, string PathName) {
        bool StandardModeIsChecked = (bool)ConfMainWindow.StandardMode.IsChecked, StrongestModeIsChecked = (bool)ConfMainWindow.StrongestMode.IsChecked;
        using (TextWriter writerSync = TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("HH.mm.ss_") + System.IO.Path.GetFileName(PathName) + ".log", false, System.Text.Encoding.GetEncoding("shift_jis")))) {
            IEnumerable<string> PNGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.png", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//stop watch get time
            if (PNGFiles.Any()) {
                sw.Start();
                Parallel.ForEach(PNGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                    CutPNGMarginMain(StandardModeIsChecked, StrongestModeIsChecked, in f, writerSync);
                });
                writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                sw.Stop();
                ConfMainWindow.FolderLog.Text += ("PNGWhiteRemove:" + sw.Elapsed + "\n");
            }
            IEnumerable<string> JPGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            if (JPGFiles.Any()) {
                sw.Restart();
                Parallel.ForEach(JPGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                    CutJPGMarginMain(StandardModeIsChecked, StrongestModeIsChecked, in f, writerSync);
                });/*-*/
                writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                sw.Stop();
                ConfMainWindow.FolderLog.Text += ("JPGWhiteRemove:" + sw.Elapsed + "\n");
            }
        }
    }
    private static byte GetConcentrationThreshold(in byte[] Histgram, bool StandardModeIsChecked) {
        return (byte)((Histgram.Max() - Histgram.Min()) * GetMangaTextConst(StandardModeIsChecked) / Image.Const.Tone8Bit);
    }
    private static double GetMangaTextConst(bool StandardModeIsChecked) {//図表がマンガ 小説がText それぞれ画像密度が違うので 閾値を変更したい、
        return StandardModeIsChecked ? Cleanliness.Clean : Cleanliness.Dirty;//Clean:小説Text,Dirty:図表マンガ 25=256*10%
    }
    public static int GetShortSide(Mat p_img) {//辺の長い方を取得
        return p_img.Width > p_img.Height ? p_img.Width : p_img.Height;
    }
    private static int GetRangeMedianF(Mat p_img) {
        return StandardAlgorithm.Math.MakeItOdd((int)System.Math.Sqrt(System.Math.Sqrt(GetShortSide(p_img) + 80)));//短辺+80の四乗根
    }
    private static void MedianLaplacianMedian(bool StandardModeIsChecked, bool StrongestModeIsChecked, Mat InputGrayImage, Mat LaplacianImage) {
        if (StandardModeIsChecked) {
            Cv2.Laplacian(InputGrayImage, LaplacianImage, MatType.CV_8U);
            return;
        } else {//図表マンガ メディアンフィルタ実行 画像サイズに応じてマスクサイズを決める
            Mat MedianImage = new Mat();
            Cv2.MedianBlur(InputGrayImage, MedianImage, GetRangeMedianF(InputGrayImage));//Image.FastestMedian(InputGrayImage, MedianImage, GetRangeMedianF(InputGrayImage));
            Cv2.Laplacian(MedianImage, LaplacianImage, MatType.CV_8U);
            MedianImage.Dispose();
            Cv2.MedianBlur(LaplacianImage, LaplacianImage, 3);
        }
        if (StrongestModeIsChecked) {//StrongModeではオ－プニング処理を追加し，ゴミ微小領域を消滅する
            Mat element = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(3, 3), new OpenCvSharp.Point(1, 1));
            Cv2.MorphologyEx(LaplacianImage, LaplacianImage, MorphTypes‎.Open, element, null, 1, BorderTypes‎.Reflect);//input output,種類, 矩形,端部処理
            element.Dispose();
        }
    }
    private static void CarmineCliAuto(in string PathName) {//ハフマンテーブルの最適化によってjpgサイズを縮小
        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only jpg files under the path.
        if (files.Any())
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => StandardAlgorithm.ExecuteAnotherApp("carmine_cli.exe", "\"" + f + "\" -o", false, true));//マルチスレッド化するのでファイル毎
    }
    private static void ExecutePNGout(Tanaka.MainWindow ConfMainWindow, in string PathName) {
        IEnumerable<string> PNGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.png", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
        if (PNGFiles.Any()) {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//stop watch get time
            sw.Start();
            Parallel.ForEach(PNGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {
                StandardAlgorithm.ExecuteAnotherApp("pngout.exe", "\"" + f + "\"", false, true);//PNGOptimize
            });
            sw.Stop();
            ConfMainWindow.FolderLog.Text += ("pngout:" + sw.Elapsed + "\n");
        }
    }
    private static unsafe void NoiseRemoveBlockTwoArea(in string f, byte max) {
        Mat p_img = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat q_img = p_img.Clone();
        byte* p = p_img.DataPointer; byte* q = q_img.DataPointer;
        for (int y = 0; y < q_img.Height * q_img.Width; ++y)
            q[y] = q[y] < max ? (byte)0 : (byte)255;//First, binarize
        for (int y = 1; y < q_img.Height - 1; ++y) {
            int yoffset = (q_img.Width * y);
            for (int x = 1; x < q_img.Width - 1; ++x)
                if (q[yoffset + x] == 0)//Count white spots around black dots
                    for (int yy = -1; yy < 2; ++yy) {
                        int yyyoffset = q_img.Width * (y + yy);
                        for (int xx = -1; xx < 2; ++xx)
                            if (q[yyyoffset + (x + xx)] == 255)
                                ++q[yoffset + x];
                    }//
        }
        for (int y = 1; y < q_img.Height - 1; ++y) {
            int yoffset = (q_img.Width * y);
            for (int x = 1; x < q_img.Width - 1; ++x) {
                if (q[yoffset + x] == 7)//When there are seven white spots in the periphery
                    for (int yy = -1; yy < 2; ++yy) {
                        int yyyoffset = q_img.Width * (y + yy);
                        for (int xx = -1; xx < 2; ++xx) {
                            int offset = yyyoffset + (x + xx);
                            if (q[offset] == 7) {//仲間 ペア
                                p[yoffset + x] = max;//q[yoffset+p_img.NChannels*x]=6;//Unnecessary
                                p[offset] = max;
                                q[offset] = 6;
                                yy = 1;
                                break;
                            }
                        }
                    }
                else if (q[yoffset + x] == 8)
                    p[yoffset + x] = max;//Independent
            }
        }
        Cv2.ImWrite(f, p_img, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        //ShowImage(nameof(p_img),p_img);
        q_img.Dispose();
        p_img.Dispose();
    }
    private static unsafe void NoiseRemoveWhite(in string f, byte min) {
        Mat p_img = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat q_img = p_img.Clone();
        byte* p = p_img.DataPointer; byte* q = q_img.DataPointer;
        for (int y = 0; y < q_img.Height * q_img.Width; ++y)
            q[y] = p[y] > min ? (byte)255 : (byte)0;//First, binarize
        for (int y = 1; y < q_img.Height - 1; ++y) {
            int yoffset = (q_img.Width * y);
            for (int x = 1; x < q_img.Width - 1; ++x)
                if (q[yoffset + x] == 0)//Count white spots around black dots
                    for (int yy = -1; yy < 2; ++yy) {
                        int yyyoffset = q_img.Width * (y + yy);
                        for (int xx = -1; xx < 2; ++xx)
                            if (q[yyyoffset + (x + xx)] == 0)
                                ++q[yoffset + x];
                    }
        }
        for (int y = 1; y < q_img.Height - 1; ++y) {
            int yoffset = (q_img.Width * y);
            for (int x = 1; x < q_img.Width - 1; ++x) {
                /*if(q[yoffset+x]==7)//When there are seven white spots in the periphery
                    for(int yy=-1;yy<2;++yy) {
                        int yyyoffset = q_img.Width*(y+yy);
                        for(int xx=-1;xx<2;++xx) {
                            int offset=yyyoffset+(x+xx);
                            if(q[offset]==7) {//仲間 ペア
                                p[yoffset+x]=min;//q[yoffset+p_img.NChannels*x]=6;//Unnecessary
                                p[offset]=min;q[offset]=6;
                                yy=1;break;
                            } else;
                        }
                    }
                else/**/
                if (q[yoffset + x] == 8)
                    p[yoffset + x] = min;//Independent
            }
        }
        Cv2.ImWrite(f, p_img, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        q_img.Dispose();
        p_img.Dispose();
    }
    private static int GetNewHeightWidth(int[] TargetXColumnYRow, int HeightWidth, int InstanceThreshold) {
        int NewHeightWidth = 0;
        for (int xory = 0; xory < HeightWidth; ++xory) {
            if (TargetXColumnYRow[xory] > InstanceThreshold)//実態あり
                ++NewHeightWidth;
        }
        if (NewHeightWidth < 1)
            ++NewHeightWidth;
        return NewHeightWidth;
    }
    private static unsafe bool UselessYRowSpacingDeletion(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat LaplacianImage = new Mat();
        Cv2.Laplacian(InputGrayImage, LaplacianImage, InputGrayImage.Depth());
        byte* p = LaplacianImage.DataPointer;
        int[] TargetYRow = new int[LaplacianImage.Height];//TargetYRow[y]が閾値以下ならその行を削除
        for (int y = 0; y < LaplacianImage.Height; y++)
            for (int x = 0; x < LaplacianImage.Width; x++)
                if (p[LaplacianImage.Width * y + x] > 0) {
                    ++TargetYRow[y];
                }
        int InstanceThreshold = 0;
        Mat OutputCutImage = new Mat(GetNewHeightWidth(TargetYRow, LaplacianImage.Height, InstanceThreshold), InputGrayImage.Width, InputGrayImage.Depth(), Image.Is.GrayScale);

        byte* src = InputGrayImage.DataPointer; byte* dst = OutputCutImage.DataPointer;
        for (int x = 0; x < InputGrayImage.Width; x++) {
            int ValidYs = 0;//有効なYの数
            for (int y = 0; y < InputGrayImage.Height; y++) {
                if (TargetYRow[y] > InstanceThreshold) {//実態あり
                    dst[OutputCutImage.Width * ValidYs + x] = src[InputGrayImage.Width * y + x];
                    ++ValidYs;
                }
            }
        }
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        InputGrayImage.Dispose();
        LaplacianImage.Dispose();
        OutputCutImage.Dispose();
        return true;
    }
    private static unsafe bool UselessXColumSpacingDeletion(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat LaplacianImage = new Mat();
        Cv2.Laplacian(InputGrayImage, LaplacianImage, MatType.CV_8U);
        byte* p = LaplacianImage.DataPointer;
        int[] TargetXColumn = new int[LaplacianImage.Width];//TargetRow[x]が閾値以下ならその行を削除
        for (int y = 0; y < LaplacianImage.Height; y++)
            for (int x = 0; x < LaplacianImage.Width; x++)
                if (p[LaplacianImage.Width * y + x] > 0) {
                    ++TargetXColumn[x];
                }
        int InstanceThreshold = 0;
        Mat OutputCutImage = new Mat(InputGrayImage.Height, GetNewHeightWidth(TargetXColumn, LaplacianImage.Width, InstanceThreshold), MatType.CV_8U, Image.Is.GrayScale);
        byte* src = InputGrayImage.DataPointer; byte* dst = OutputCutImage.DataPointer;
        for (int y = 0; y < InputGrayImage.Height; y++) {
            int ValidXs = 0;//有効なXの数
            for (int x = 0; x < InputGrayImage.Width; x++) {
                if (TargetXColumn[x] > InstanceThreshold) {//実態あり
                    dst[OutputCutImage.Width * y + ValidXs] = src[InputGrayImage.Width * y + x];
                    ++ValidXs;
                }
            }
        }
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        InputGrayImage.Dispose();
        LaplacianImage.Dispose();
        OutputCutImage.Dispose();
        return true;
    }
    private static unsafe void FixPixelMissing(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);
        Mat FixedImage = InputGrayImage.Clone();
        byte* src = InputGrayImage.DataPointer; byte* dst = FixedImage.DataPointer;
        for (int y = 2; y < InputGrayImage.Height - 2; ++y) {
            int yoffset = InputGrayImage.Cols * y;
            for (int x = 2; x < InputGrayImage.Cols - 2; ++x) {
                int offset = yoffset + x;//current position
                byte NeighborValue = src[offset + 1];//隣の奴をキャッシュに取っておく
                if ((src[offset] != (NeighborValue)) && ((NeighborValue) == (src[offset - 1])) && ((NeighborValue) == (src[offset + 2])) && ((NeighborValue) == (src[offset - InputGrayImage.Cols])) && ((NeighborValue) == (src[offset + InputGrayImage.Cols])) && ((src[offset - 2]) == (NeighborValue)) && ((src[offset - 2 * InputGrayImage.Cols]) == (NeighborValue)) && ((src[offset + 2 * InputGrayImage.Cols]) == (NeighborValue)))
                    dst[offset] = (NeighborValue);
            }
        }
        Cv2.ImWrite(f, FixedImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        FixedImage.Dispose();
        InputGrayImage.Dispose();
    }
    private static bool CompareArrayAnd(int ___Threshold___, int[] ___CompareArray___) {
        foreach (int ___CompareValue___ in ___CompareArray___) {
            if (___Threshold___ > ___CompareValue___)
                continue;
            else
                return false;
        }
        return true;
    }
    private static unsafe bool GetYLow(Mat p_img, Threshold ImageThreshold, ImageRect NewImageRect) {
        byte* p = p_img.DataPointer;
        int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
        for (int yy = 0; yy <= Var.MaxMarginSize; ++yy)
            for (int x = 0; x < p_img.Width; ++x)
                if (p[p_img.Width * yy + x] < ImageThreshold.Concentration)
                    ++TargetRowArray[yy];
        if (CompareArrayAnd(ImageThreshold.Width, TargetRowArray)) {
            NewImageRect.YLow = 0;
            return true;
        }
        for (int y = 1; y < p_img.Height - Var.MaxMarginSize; y++) {
            int TargetRow = 0;
            for (int x = 0; x < p_img.Width; x++)
                if (p[p_img.Width * (y + Var.MaxMarginSize) + x] < ImageThreshold.Concentration)
                    ++TargetRow;
            if (ImageThreshold.Width > TargetRow) {
                NewImageRect.YLow = y - Var.MaxMarginSize < 0 ? 0 : y - Var.MaxMarginSize;
                return true;
            }
        }
        return false;//絶対到達しない
    }
    private static unsafe bool GetYHigh(Mat p_img, Threshold ImageThreshold, ImageRect NewImageRect) {
        byte* p = p_img.DataPointer;
        int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
        for (int yy = -Var.MaxMarginSize; yy < 1; ++yy)
            for (int x = 0; x < p_img.Width; ++x)
                if (p[p_img.Width * ((p_img.Height - 1) + yy) + x] < ImageThreshold.Concentration)
                    ++TargetRowArray[-yy];
        if (CompareArrayAnd(ImageThreshold.Width, TargetRowArray)) {
            NewImageRect.YHigh = p_img.Height;//prb
            return true;
        }
        for (int y = p_img.Height - 2; y > (NewImageRect.YLow + Var.MaxMarginSize); --y) {//Y下取得
            int TargetRow = 0;
            for (int x = 0; x < p_img.Width; ++x)
                if (p[p_img.Width * (y - Var.MaxMarginSize) + x] < ImageThreshold.Concentration)
                    ++TargetRow;
            if ((ImageThreshold.Width > TargetRow)) {
                NewImageRect.YHigh = y + Var.MaxMarginSize > p_img.Height ? p_img.Height : y + Var.MaxMarginSize;//prb
                return true;
            }
        }
        return false;//絶対到達しない
    }
    private static unsafe bool GetXLow(Mat p_img, Threshold ImageThreshold, ImageRect NewImageRect) {
        byte* p = p_img.DataPointer;
        int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
        for (int xx = 0; xx <= Var.MaxMarginSize; ++xx)
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                if (p[xx + p_img.Width * y] < ImageThreshold.Concentration)
                    ++TargetRowArray[xx];
        if (CompareArrayAnd(ImageThreshold.Height, TargetRowArray)) {
            NewImageRect.XLow = 0;
            return true;
        }
        for (int x = 0; x < p_img.Width - Var.MaxMarginSize; x++) {//X左取得
            int TargetRow = 0;
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                if (p[x + Var.MaxMarginSize + p_img.Width * y] < ImageThreshold.Concentration)
                    ++TargetRow;
            if (ImageThreshold.Height > TargetRow) {
                NewImageRect.XLow = x - Var.MaxMarginSize < 0 ? 0 : x - Var.MaxMarginSize;
                return true;
            }
        }
        return false;//絶対到達しない
    }
    private static unsafe bool GetXHigh(Mat p_img, Threshold ImageThreshold, ImageRect NewImageRect) {
        byte* p = p_img.DataPointer;
        int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
        for (int xx = -Var.MaxMarginSize; xx < 1; ++xx)
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                if (p[((p_img.Width - 1) + xx) + p_img.Width * y] < ImageThreshold.Concentration)
                    ++TargetRowArray[-xx];
        if (CompareArrayAnd(ImageThreshold.Height, TargetRowArray)) {
            NewImageRect.XHigh = p_img.Width; //prb
            return true;
        }
        for (int x = p_img.Width - 2; x > NewImageRect.XLow + Var.MaxMarginSize; --x) {//X右取得
            int TargetRow = 0;
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                if (p[x - Var.MaxMarginSize + p_img.Width * y] < ImageThreshold.Concentration)
                    ++TargetRow;
            if (ImageThreshold.Height > TargetRow) {
                NewImageRect.XHigh = x + Var.MaxMarginSize > p_img.Width ? p_img.Width : x + Var.MaxMarginSize;//prb
                return true;
            }
        }
        return false;
    }
    private static bool GetNewImageSize(Mat p_img, Threshold ImageThreshold, ImageRect NewImageRect) {
        ImageThreshold.Width = p_img.Width - ImageThreshold.WidthTimes;
        if (!GetYLow(p_img, ImageThreshold, NewImageRect))//Y上取得
            return false;
        if (!GetYHigh(p_img, ImageThreshold, NewImageRect))//X左
            return false;
        NewImageRect.Height = NewImageRect.YHigh - NewImageRect.YLow;
        ImageThreshold.Height = NewImageRect.Height - ImageThreshold.HeightTimes;
        if (!GetXLow(p_img, ImageThreshold, NewImageRect))//Y下取得
            return false;
        if (!GetXHigh(p_img, ImageThreshold, NewImageRect))//X右
            return false;
        NewImageRect.Width = NewImageRect.XHigh - NewImageRect.XLow;
        return true;
    }
}