using System;
using System.IO;
using System.Linq;//enum
using System.Collections.Generic;//enum
using System.Text.RegularExpressions;//正規表現
using Tanaka;
public class EntryPoint {
    public void FileOrFolder(MainWindow ConfMainWindow, System.Collections.Specialized.StringCollection filespath) {
        foreach (string PathName in filespath) {//Enumerate acquired paths
            ConfMainWindow.FolderLog.Text += "\n" + PathName;
            ConfMainWindow.FilesLog.Text += "\n" + PathName;//Show path
            if (File.GetAttributes(PathName).HasFlag(FileAttributes.Directory)) {//フォルダ //JudgeFileOrDirectory
                if ((bool)ConfMainWindow.ExecutFilesRename.IsChecked)//リネームするか？
                    if (!FilesRename.RenameFiles(ConfMainWindow, in PathName))
                        return;//リネーム失敗
                long[] FilesSize = new long[2];
                FilesSize[0] = StandardAlgorithm.Directory.GetDirectorySize(new DirectoryInfo(PathName));
                if ((bool)ConfMainWindow.MarginRemove.IsChecked) {
                    ImageAlgorithm.RemoveMarginEntry(ConfMainWindow, PathName);
                }
                if ((bool)ConfMainWindow.PNGout.IsChecked)
                    ImageAlgorithm.ExecutePNGout(ConfMainWindow, in PathName);
                ImageAlgorithm.CarmineCliAuto(in PathName);
                FilesSize[1] = StandardAlgorithm.Directory.GetDirectorySize(new DirectoryInfo(PathName));
                DiplayFilesSize(ConfMainWindow, FilesSize);
                if (!(bool)ConfMainWindow.NotArchive.IsChecked)
                    CreateZip(ConfMainWindow, PathName);
            } else {//ファイルはnewをつくりそこで実行
                string NewPath = System.IO.Path.GetDirectoryName(PathName) + "\\new\\";
                System.IO.Directory.CreateDirectory(NewPath);//"\\new"
                System.IO.File.Copy(PathName, NewPath + Path.GetFileName(PathName), true);//"\\new\\hoge.jpg"
                if ((bool)ConfMainWindow.MarginRemove.IsChecked)
                    ImageAlgorithm.RemoveMarginEntry(ConfMainWindow, NewPath);//該当ファイルのあるフォルダの奴はすべて実行される別フォルダに単体コピーが理想*/
                if ((bool)ConfMainWindow.PNGout.IsChecked)
                    ImageAlgorithm.ExecutePNGout(ConfMainWindow, in NewPath);
                ImageAlgorithm.CarmineCliAuto(in NewPath);
            }
        }
        CompressLogsWith7z(ConfMainWindow);
    }
    private void CompressLogsWith7z(MainWindow ConfMainWindow) {
        using (TextWriter writerSync = TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("HH.mm.ss") + ".log", false, System.Text.Encoding.GetEncoding("shift_jis")))) {
            writerSync.WriteLine(ConfMainWindow.FolderLog.Text);
            writerSync.WriteLine(ConfMainWindow.FilesLog.Text);//richTextBox1
        }
        StandardAlgorithm.ExecuteAnotherApp("7z.exe", "a " + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".7z *.log -sdel -mx9", false, true);
    }
    private void DiplayFilesSize(MainWindow ConfMainWindow, long[] FilesSize) {
        ConfMainWindow.FolderLog.Text += "BeforeFileSize:" + FilesSize[0] + " Byte\n";
        ConfMainWindow.FolderLog.Text += "AfterFilesaize:" + FilesSize[1] + " Byte";//Magnification
        ConfMainWindow.FolderLog.Text += "\nMagnification:" + ((double)FilesSize[1] / FilesSize[0]) * 100 + " %";
    }
    private void CreateZip(MainWindow ConfMainWindow, string PathName) {
        string Extension = ".zip";
        string FileName = "Rar.exe";
        string Arguments;
        string CompressLevel = " -m5 ";//compress level max rar
        if ((bool)ConfMainWindow.Rar.IsChecked) {//winrar
            Extension = ".rar";
            if ((bool)ConfMainWindow.None.IsChecked)//non compress
                CompressLevel = " -m0 ";
            Arguments = " a \"" + PathName + Extension + "\" -rr5 -mt" + System.Environment.ProcessorCount + CompressLevel + "-ep \"" + PathName + "\"";
            /*a             書庫にファイルを圧縮
              rr[N]         リカバリレコードを付加
              m<0..5>       圧縮方式を指定 (0-無圧縮...5-標準...5-最高圧縮)
              mt<threads>   スレッドの数をセット
              ep            名前からパスを除外*/
        } else {
            FileName = "7z.exe";
            CompressLevel = " ";//compress level default zip 7z
            if ((bool)ConfMainWindow.SevenZip.IsChecked)
                Extension = ".7z";
            if ((bool)ConfMainWindow.None.IsChecked)
                CompressLevel = " -mx0 ";//compress non
            else if ((bool)ConfMainWindow.Max.IsChecked)
                CompressLevel = " -mx9 ";//compress level max
            Arguments = "a \"" + PathName + Extension + "\" -mmt=on" + CompressLevel + "\"" + PathName + "\\*\"";
        }
        StandardAlgorithm.ExecuteAnotherApp(in FileName, in Arguments, false, true);
        RenameNumberOnlyFile(ConfMainWindow, PathName, Extension);
    }
    private string GetNumberOnlyPath(string PathName) {//ファイル名からX巻のXのみを返す
        string FileName = System.IO.Path.GetFileName(PathName);//Z:\[宮下英樹] センゴク権兵衛 第05巻 ->[宮下英樹] センゴク権兵衛 第05巻
        Match MatchedNumber = Regex.Match(FileName, "(\\d)+巻");//[宮下英樹] センゴク権兵衛 第05巻 ->05巻
        if (MatchedNumber.Success)
            MatchedNumber = Regex.Match(MatchedNumber.Value, "(\\d)+");//05巻->05
        else {
            MatchedNumber = Regex.Match(FileName, "(\\d)+");//[宮下英樹] センゴク権兵衛 第05 ->05
            if (!MatchedNumber.Success)
                return PathName;//[宮下英樹] センゴク権兵衛 第 ->
        }
        //文字列を置換する（FileNameをMatchedNumber.Valueに置換する）
        return PathName.Replace(FileName, int.Parse(MatchedNumber.Value).ToString());//Z:\5
    }
    private void RenameNumberOnlyFile(MainWindow ConfMainWindow, string PathName, string Extension) {
        string NewFileName = GetNumberOnlyPath(PathName) + Extension;
        if (!System.IO.File.Exists(NewFileName))//重複
            File.Move(PathName + Extension, NewFileName);//重複してない
        ConfMainWindow.FolderLog.Text += "\nCreated " + NewFileName + ".\n";//Show path
    }
}