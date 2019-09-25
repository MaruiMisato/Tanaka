using System.Threading.Tasks;//setparalle
using System.Linq;//enum
using System.Collections.Generic;//enum
using Tanaka;
using OpenCvSharp;
using System.Windows.Media;
using System;
using System.Windows.Media.Imaging;
using System.IO;

public class ImageAlgorithm {
    static class Var {//配列の宣言で使うことが多い 不変普遍定数ではない　ユーザが勝手に変えろ
        public const int MaxMarginSize = 4;//実際は＋1
    }
    private static void ShowImage(string WindowName, Mat src) {
        Cv2.ImShow(WindowName, src);
        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }
    public class ImageRect//画像固有の値だからstaicではない
    {
        public int YLow { get; set; }
        public int XLow { get; set; }
        public int YHigh { get; set; }
        public int XHigh { get; set; }
        public int Height { get; set; }//YHigh-YLow
        public int Width { get; set; }//XHigh-XLow
    }
    public class Threshold//画像固有の値だからstaicではない
    {
        public byte Concentration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Times { get; } = 3;
    }

    private static bool CutPNGMarginMain(bool StandardModeIsChecked, bool StrongestModeIsChecked, in string f, TextWriter writerSync) {
        byte[] OriginHistgram = new byte[Image.Const.Tone8Bit];
        if (Image.GetHistgramR(in f, ref OriginHistgram) == Image.Is.Color) {//カラーでドット埋めは無理
        } else {
            FixPixelMissing(in f);//ピクセル欠けを修正
            NoiseRemoveTwoArea(in f, OriginHistgram.Max());//小さいゴミ削除
            NoiseRemoveWhite(in f, OriginHistgram.Min());//小さいゴミ削除
            UselessXColumSpacingDeletion(in f);//空白列削除
            UselessYRowSpacingDeletion(in f);//空白行削除
        }
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);
        Mat LaplacianImage = new Mat(InputGrayImage.Height, InputGrayImage.Width, MatType.CV_8U);//IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
        MedianLaplacianMedian(StandardModeIsChecked, StrongestModeIsChecked, InputGrayImage, LaplacianImage);//MedianLaplacianMedianをかけて画像平滑化
        byte[] Histgram = new byte[Image.Const.Tone8Bit];
        int Channel = Image.GetHistgramR(in f, ref Histgram);//GetImageToneValue(f, out int Channel, out Histgram);
        if (Histgram.Max() == Histgram.Min()) {//最大値と最小値が同じなら豆腐か黒塗り．つまり処理不要，手動で削除しとけ
            System.Windows.MessageBox.Show(Histgram.Max().ToString() + Histgram.Min().ToString());
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }
        Threshold ImageThreshold = new Threshold {
            Concentration = ImageAlgorithm.GetConcentrationThreshold(in Histgram, ImageAlgorithm.GetMangaTextConst(StandardModeIsChecked))//勾配が重要？
        };
        ImageRect NewImageRect = new ImageRect();
        if (!ImageAlgorithm.GetNewImageSize(LaplacianImage, ImageThreshold, NewImageRect)) {
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }
        LaplacianImage.Dispose();
        writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + "), (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Width + "," + NewImageRect.Height + ")" + " threshold=" + ImageThreshold.Concentration + ",Min=" + Histgram.Min() + ",Max=" + Histgram.Max());
        Mat OutputCutImage = new Mat(NewImageRect.Height, NewImageRect.Width, MatType.CV_8U, Channel);//IplImage OutputCutImage = Cv.CreateImage(NewImageRect.Size, BitDepth.U8, Channel);
        if (Channel == Image.Is.GrayScale) {
            OutputCutImage = InputGrayImage.Clone(new OpenCvSharp.Rect(NewImageRect.XLow, NewImageRect.YLow, NewImageRect.Width, NewImageRect.Height));//WhiteCut(InputGrayImage, OutputCutImage, NewImageRect);
            InputGrayImage.Dispose();
            Image.Transform2Linear(OutputCutImage, Histgram.Max(), Histgram.Min());// 階調値変換
        } else {//Is.Color
            Mat InputColorImage = Cv2.ImRead(f, ImreadModes.Color);//IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);
            OutputCutImage = InputColorImage.Clone(new OpenCvSharp.Rect(NewImageRect.XLow, NewImageRect.YLow, NewImageRect.Width, NewImageRect.Height));//WhiteCut(InputGrayImage, OutputCutImage,
            InputColorImage.Dispose();
        }
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));//Cv.SaveImage(f, OutputCutImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
        OutputCutImage.Dispose();
        return true;
    }
    private static bool CutJPGMarginMain(bool StandardModeIsChecked, bool StrongestModeIsChecked, in string f, TextWriter writerSync) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
        Mat LaplacianImage = new Mat(InputGrayImage.Height, InputGrayImage.Width, MatType.CV_8U);//IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
        ImageAlgorithm.MedianLaplacianMedian(StandardModeIsChecked, StrongestModeIsChecked, InputGrayImage, LaplacianImage);
        byte[] Histgram = new byte[Image.Const.Tone8Bit];
        int Channel = Image.GetHistgramR(in f, ref Histgram);//GetImageToneValue(f, out int Channel, out Histgram);
        if (Histgram.Max() == Histgram.Min()) {//最大値と最小値が同じなら豆腐か黒塗り．つまり処理不要，手動で削除しとけ
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }
        //
        ImageRect NewImageRect = new ImageRect();
        if (!GetNewImageSize(LaplacianImage, new Threshold { Concentration = GetConcentrationThreshold(in Histgram, GetMangaTextConst(StandardModeIsChecked)) }, NewImageRect)) {
            InputGrayImage.Dispose();
            LaplacianImage.Dispose();
            return false;
        }
        writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + "), (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Width + "," + NewImageRect.Height + ")");//prb
        //jpegtran.exe -crop 808x1208+0+63 -outfile Z:\bin\22\6.jpg Z:\bin\22\6.jpg
        InputGrayImage.Dispose();
        LaplacianImage.Dispose();
        string Arguments = "-crop " + NewImageRect.Width + "x" + NewImageRect.Height + "+" + NewImageRect.XLow + "+" + NewImageRect.YLow + " -progressive -outfile \"" + f + "\" \"" + f + "\"";
        StandardAlgorithm.ExecuteAnotherApp("jpegtran.exe", in Arguments, false, true);
        return true;
    }
    public static void RemoveMarginEntry(MainWindow ConfMainWindow, string PathName) {
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
    private static byte GetConcentrationThreshold(in byte[] Histgram, double MangaTextConst) {
        return (byte)((Histgram.Max() - Histgram.Min()) * MangaTextConst / Image.Const.Tone8Bit);
    }
    private static double GetMangaTextConst(bool StandardModeIsChecked) {//図表がマンガ 小説がText それぞれ画像密度が違うので 閾値を変更したい、
        if (StandardModeIsChecked) {//故にこの定数を使って閾値を変える
            return 15;//小説Text
        } else {
            return 25;//図表マンガ 25=256*10%
        }
    }
    public static int GetShortSide(Mat p_img) {
        return p_img.Width > p_img.Height ? p_img.Width : p_img.Height;
    }
    private static int GetRangeMedianF(Mat p_img) {
        return StandardAlgorithm.Math.MakeItOdd((int)System.Math.Sqrt(System.Math.Sqrt(GetShortSide(p_img) + 80)));//短辺+80の四乗根
    }
    private static bool MedianLaplacianMedian(bool StandardModeIsChecked, bool StrongestModeIsChecked, Mat InputGrayImage, Mat LaplacianImage) {
        //Mat MedianImage = new Mat(InputGrayImage.Height, InputGrayImage.Width, MatType.CV_8U);// IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
        Mat MedianImage = new Mat();// IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
        if (StandardModeIsChecked) {
            MedianImage = InputGrayImage.Clone();//Cv.Copy(InputGrayImage, MedianImage);//小説Textはメディアンフィルタ適用外
        } else {//図表マンガ メディアンフィルタ実行 画像サイズに応じてマスクサイズを決める
            Cv2.MedianBlur(InputGrayImage, MedianImage, GetRangeMedianF(InputGrayImage));
            //Image.FastestMedian(InputGrayImage, MedianImage, GetRangeMedianF(InputGrayImage));
        }
        Cv2.Laplacian(MedianImage, LaplacianImage, MatType.CV_8U);//Cv.Laplace(MedianImage, LaplacianImage, ApertureSize.Size1);
#if (DEBUG_SAVE)
#endif
#if (DEBUG_DISPLAY)
#endif
        if (StandardModeIsChecked) {
            //Image.Filter.FastestMedian(LaplacianImage, 0);//小説Textはメディアンフィルタ適用外
        } else {//図表マンガ メディアンフィルタ実行 画像サイズに応じてマスクサイズを決める
            Cv2.MedianBlur(LaplacianImage, LaplacianImage, 3);//Image.Filter.FastestMedian(LaplacianImage, GetRangeMedianF(LaplacianImage));
        }
        if (StrongestModeIsChecked) {//StrongModeではオ－プニング処理を追加し，ゴミ微小領域を消滅する
            Mat element = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(3, 3), new OpenCvSharp.Point(1, 1));//IplConvKernel element = Cv.CreateStructuringElementEx(3, 3, 1, 1, ElementShape.Rect, null);
            Cv2.MorphologyEx(LaplacianImage, LaplacianImage, MorphTypes‎.Open, element, null, 1, BorderTypes‎.Reflect);//Cv.MorphologyEx(LaplacianImage, LaplacianImage, MedianImage, element, MorphologyOperation.Open, 1);//input output temp 矩形,種類,回数
            element.Dispose();
        }
        MedianImage.Dispose();/*-*/
        return true;
    }
    public static void CarmineCliAuto(in string PathName) {//ハフマンテーブルの最適化によってjpgサイズを縮小
        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only jpg files under the path.
        if (files.Any())
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => StandardAlgorithm.ExecuteAnotherApp("carmine_cli.exe", "\"" + f + "\" -o", false, true));//マルチスレッド化するのでファイル毎
    }
    public static void ExecutePNGout(Tanaka.MainWindow ConfMainWindow, in string PathName) {
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
    private static unsafe void NoiseRemoveTwoArea(in string f, byte max) {
        Mat p_img = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage p_img = Cv.LoadImage(f,LoadMode.GrayScale);
        Mat q_img = p_img.Clone();//IplImage q_img = Cv.CreateImage(p_img.Size,BitDepth.U8,1);//Cv.Copy(p_img,q_img);
        byte* p = p_img.DataPointer; byte* q = q_img.DataPointer;//byte* p = (byte*)p_img.ImageData, q = (byte*)q_img.ImageData;
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
        Cv2.ImWrite(f, p_img, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));//Cv.SaveImage(f,p_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));

        //ShowImage(nameof(p_img),p_img);
        q_img.Dispose();
        p_img.Dispose();
    }
    private static unsafe void NoiseRemoveWhite(in string f, byte min) {

        Mat p_img = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage p_img = Cv.LoadImage(f,LoadMode.GrayScale);
        Mat q_img = p_img.Clone();//IplImage q_img = Cv.CreateImage(p_img.Size,BitDepth.U8,1);//Cv.Copy(p_img,q_img);
        byte* p = p_img.DataPointer; byte* q = q_img.DataPointer;//byte* p = (byte*)p_img.ImageData, q = (byte*)q_img.ImageData;
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
        Cv2.ImWrite(f, p_img, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));//Cv.SaveImage(f,p_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
        q_img.Dispose();
        p_img.Dispose();
    }
    private static int GetNewHeightWidth(int[] TargetXColumnYRow, int HeightWidth, int InstanceThreshold) {
        int NewHeightWidth = 0;
        for (int xory = 0; xory < HeightWidth; ++xory) {
            if (TargetXColumnYRow[xory] > InstanceThreshold)//実態あり
                ++NewHeightWidth;
        }
        return NewHeightWidth;
    }
    private static unsafe bool UselessYRowSpacingDeletion(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage InputGrayImage = Cv.LoadImage(f,LoadMode.GrayScale);//
        Mat LaplacianImage = new Mat();//IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size,BitDepth.U8,1);
        Cv2.Laplacian(InputGrayImage, LaplacianImage, MatType.CV_8U);//Cv.Laplace(InputGrayImage,LaplacianImage,ApertureSize.Size1);
        byte* p = LaplacianImage.DataPointer;//byte* p = (byte*)LaplacianImage.ImageData;
        int[] TargetYRow = new int[LaplacianImage.Height];//TargetYRow[y]が閾値以下ならその行を削除
        for (int y = 0; y < LaplacianImage.Height; y++)
            for (int x = 0; x < LaplacianImage.Width; x++)
                if (p[LaplacianImage.Width * y + x] > 0) {
                    ++TargetYRow[y];
                }
        int InstanceThreshold = 0;
        Mat OutputCutImage = new Mat(GetNewHeightWidth(TargetYRow, LaplacianImage.Height, InstanceThreshold), InputGrayImage.Width, MatType.CV_8U, Image.Is.GrayScale);
        //IplImage OutputCutImage = Cv.CreateImage(new CvSize(InputGrayImage.Width,GetNewHeightWidth(TargetYRow,LaplacianImage.Height,InstanceThreshold)),BitDepth.U8,Is.GrayScale);
        //byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)OutputCutImage.ImageData;
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
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));//Cv.SaveImage(f,OutputCutImage,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
        InputGrayImage.Dispose();
        LaplacianImage.Dispose();
        OutputCutImage.Dispose();
        return true;
    }
    private static unsafe bool UselessXColumSpacingDeletion(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale);//IplImage InputGrayImage = Cv.LoadImage(f,LoadMode.GrayScale);//
        Mat LaplacianImage = new Mat();//IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size,BitDepth.U8,1);
        Cv2.Laplacian(InputGrayImage, LaplacianImage, MatType.CV_8U);//Cv.Laplace(InputGrayImage,LaplacianImage,ApertureSize.Size1);
        byte* p = LaplacianImage.DataPointer;//byte* p = (byte*)LaplacianImage.ImageData;
        int[] TargetXColumn = new int[LaplacianImage.Width];//TargetRow[x]が閾値以下ならその行を削除
        for (int y = 0; y < LaplacianImage.Height; y++)
            for (int x = 0; x < LaplacianImage.Width; x++)
                if (p[LaplacianImage.Width * y + x] > 0) {
                    ++TargetXColumn[x];
                }
        int InstanceThreshold = 0;
        Mat OutputCutImage = new Mat(InputGrayImage.Height, GetNewHeightWidth(TargetXColumn, LaplacianImage.Width, InstanceThreshold), MatType.CV_8U, Image.Is.GrayScale);
        //IplImage OutputCutImage = Cv.CreateImage(new CvSize(GetNewHeightWidth(TargetXColumn,LaplacianImage.Width,InstanceThreshold),InputGrayImage.Height),BitDepth.U8,Is.GrayScale);
        byte* src = InputGrayImage.DataPointer; byte* dst = OutputCutImage.DataPointer;//byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)OutputCutImage.ImageData;
        for (int y = 0; y < InputGrayImage.Height; y++) {
            int ValidXs = 0;//有効なXの数
            for (int x = 0; x < InputGrayImage.Width; x++) {
                if (TargetXColumn[x] > InstanceThreshold) {//実態あり
                    dst[OutputCutImage.Width * y + ValidXs] = src[InputGrayImage.Width * y + x];
                    ++ValidXs;
                }
            }
        }
        Cv2.ImWrite(f, OutputCutImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));//Cv.SaveImage(f,OutputCutImage,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
        InputGrayImage.Dispose();
        LaplacianImage.Dispose();
        OutputCutImage.Dispose();
        return true;
    }
    private static unsafe bool FixPixelMissing(in string f) {
        Mat InputGrayImage = Cv2.ImRead(f, ImreadModes.Grayscale); //IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
        Mat FixedImage = InputGrayImage.Clone();//IplImage FixedImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);//Cv.Copy(InputGrayImage, FixedImage);
        //byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)FixedImage.ImageData;
        //http://ni4muraano.hatenablog.com/entry/2017/04/22/161633
        byte* src = InputGrayImage.DataPointer; byte* dst = FixedImage.DataPointer;
        for (int y = 2; y < InputGrayImage.Height - 2; ++y) {
            int yoffset = InputGrayImage.Cols * y;
            for (int x = 2; x < InputGrayImage.Cols - 2; ++x) {
                int offset = (yoffset) + x;//current position
                byte offset1 = src[offset + 1];//隣の奴をキャッシュに取っておく
                if ((src[offset] != (offset1)) && ((offset1) == (src[offset - 1])) && ((offset1) == (src[offset + 2])) && ((offset1) == (src[offset - InputGrayImage.Cols])) && ((offset1) == (src[offset + InputGrayImage.Cols])) && ((src[offset - 2]) == (offset1)) && ((src[offset - 2 * InputGrayImage.Cols]) == (offset1)) && ((src[offset + 2 * InputGrayImage.Cols]) == (offset1)))
                    dst[offset] = (offset1);
            }
        }
        //byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)FixedImage.ImageData;
        //for (int y = 2; y < InputGrayImage.Height - 2; ++y) {
        //  for (int x = 2; x < InputGrayImage.Width - 2; ++x) {
        //  int offset = (InputGrayImage.Width * y) + x;//current position
        //     if (((src[offset - 1]) == (src[offset + 1])) && ((src[offset + 1]) == (src[offset - InputGrayImage.Width])) && ((src[offset + 1]) == (src[offset - InputGrayImage.Width])) && ((src[offset - 2]) == (src[offset + 1])) && ((src[offset + 2]) == (src[offset + 1])) && ((src[offset - 2 * InputGrayImage.Width]) == (src[offset + 1])) && ((src[offset + 2 * InputGrayImage.Width]) == (src[offset + 1])))
        //        dst[offset] = (src[offset + 1]);
        // }
        // }
        //Cv.SaveImage(f, FixedImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
        //https://docs.opencv.org/4.0.1/d4/da8/group__imgcodecs.html#ga292d81be8d76901bff7988d18d2b42ac
        //http://ni4muraano.hatenablog.com/entry/2017/11/20/190000
        Cv2.ImWrite(f, FixedImage, new ImageEncodingParam(ImwriteFlags.PngCompression, 0));
        FixedImage.Dispose();
        InputGrayImage.Dispose();
        return true;
    }
    private static unsafe void WhiteCutColor(in string f, Mat q_img, ImageRect NewImageRect) {//カラー画像は階調値線形変換はしない
        //var bitmapimageOriginal = new BitmapImage(new Uri(f));
        //占有しないパターン-1http://neareal.net/index.php?Programming%2F.NetFramework%2FWPF%2FWriteableBitmap%2FLoadReleaseableBitmapImage
        System.IO.MemoryStream data = new System.IO.MemoryStream(File.ReadAllBytes(f));
        WriteableBitmap wbmp = new WriteableBitmap(BitmapFrame.Create(data));
        data.Close();
        //Bitmap bmp = new Bitmap(f);
        FormatConvertedBitmap bitmap = new FormatConvertedBitmap(wbmp, PixelFormats.Pbgra32, null, 0);//32bit で読む
        byte[] originalPixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        // BitmapSourceから配列にコピー
        //https://water2litter.net/gin/?p=990
        //https://imagingsolution.net/program/csharp/bitmap-data-memory-format/
        int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
        bitmap.CopyPixels(originalPixels, stride, 0);
        byte* q = q_img.DataPointer;
        for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
            for (int x = NewImageRect.XLow; x < NewImageRect.XHigh; ++x) {
                int qoffset = (q_img.Width * (y - NewImageRect.YLow)) + (x - NewImageRect.XLow) * 3, offset = (bitmap.PixelWidth * y + x) * 4;
                q[0 + qoffset] = originalPixels[0 + offset];
                q[1 + qoffset] = originalPixels[1 + offset];
                q[2 + qoffset] = originalPixels[2 + offset];
            }
        System.Windows.MessageBox.Show("Messagebox");
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
        ImageThreshold.Width = p_img.Width - ImageThreshold.Times;
        if (!GetYLow(p_img, ImageThreshold, NewImageRect))//Y上取得
            return false;
        if (!GetYHigh(p_img, ImageThreshold, NewImageRect))//X左
            return false;
        NewImageRect.Height = NewImageRect.YHigh - NewImageRect.YLow;
        ImageThreshold.Height = NewImageRect.Height - ImageThreshold.Times;
        if (!GetXLow(p_img, ImageThreshold, NewImageRect))//Y下取得
            return false;
        if (!GetXHigh(p_img, ImageThreshold, NewImageRect))//X右
            return false;
        NewImageRect.Width = NewImageRect.XHigh - NewImageRect.XLow;
        return true;
    }
}