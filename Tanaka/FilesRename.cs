using System.Windows;
using Tanaka;
using System.IO;
using System.Collections.Generic;//enum
using System.Linq;//enum
public class FilesRename {
    public static bool RenameFiles(MainWindow ConfMainWindow, in string PathName, ref IEnumerable<string> files, string[] AllOldFileName) {
        FilesRename.GetFileNameBeforeChange(ref files, AllOldFileName);
        int NumberOfImageFiles = files.Count();
        if (!IsTheNumberOfFilesAppropriate(ConfMainWindow, NumberOfImageFiles))//個数が1000以下じゃないとリネームできない
            return false;
        if (SortFiles(ConfMainWindow, NumberOfImageFiles, in PathName, AllOldFileName)) {//ソートできるファイルか
            string[] NewFileName = new string[NumberOfImageFiles];
            CreateOrGetNewFileName(ConfMainWindow, NewFileName);
            ReNameAlfaBeta(ConfMainWindow, in PathName, ref files, NewFileName);
            return true;
        } else {
            UnSortFiles(in PathName, AllOldFileName);
            return false;
        }
    }
    private static void GetFileNameBeforeChange(ref IEnumerable<string> files, string[] AllOldFileName) {//ゴミファイルを除去 JPG jpeg PNG png種々あるので
        int NumberOfImageFiles = -1;//ファイル数をカウント
        foreach (string f in files) {
            FileInfo file = new FileInfo(f);
            if (file.Extension == ".db" || file.Extension == ".ini")
                file.Delete();//Disposal of garbage
            else//jpeg,jpg,png,PNG
                AllOldFileName[++NumberOfImageFiles] = f;//前置加算のほうが早い？
        }
    }
    private static void CreateOrGetNewFileName(MainWindow ConfMainWindow, string[] NewFileName) {
        string NewNamesFilesPath = @"NewName1000.csv";//zip under 36*25+100=1000
        if (NewFileName.Length <= 36)//一桁で0-9,a-z=36
            NewNamesFilesPath = @"NewName36.csv";
        else if ((bool)ConfMainWindow.SevenZip.IsChecked && NewFileName.Length <= 26 * 25) {//7zip under 26*25=650未満 かつ37以上
            int MaxRoot = (int)System.Math.Sqrt(NewFileName.Length) + 1;
            ConfMainWindow.FolderLog.Text += "\nroot MaxRoot" + MaxRoot;
            for (int i = 0; i < NewFileName.Length; ++i)
                NewFileName[i] = (char)((i / MaxRoot) + 'a') + ((char)(i % MaxRoot + 'a')).ToString();//26*25  36*35mezasu
            return;
        }
        using (StreamReader sr = new StreamReader(NewNamesFilesPath)) {
            for (int i = 0; i < NewFileName.Length; ++i) // 末尾まで繰り返す
                NewFileName[i] = sr.ReadLine();// CSVファイルの一行を読み込む
        }

    }
    private static void ReNameAlfaBeta(MainWindow ConfMainWindow, in string PathName, ref IEnumerable<string> files, string[] NewFileName) {
        int i = 0;
        foreach (string f in files) {
            FileInfo file = new FileInfo(f);
            string FileName = NewFileName[i] + ".png";
            if (file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".JPG" || file.Extension == ".JPEG") //jpg
                FileName = NewFileName[i] + ".jpg";
            ConfMainWindow.FilesLog.Text += (Path.GetFileNameWithoutExtension(f) + " -> " + i++ + " " + FileName + "\n");
            file.MoveTo(PathName + "/" + FileName);
        }
    }
    private static bool IsTheNumberOfFilesAppropriate(MainWindow ConfMainWindow, int NumberOfImageFiles) {
        if (NumberOfImageFiles > (36 * 25) + 100) {
            ConfMainWindow.FolderLog.Text += "\nNumberOfImageFiles:" + NumberOfImageFiles + " => over 1,000\n";
        } else if (NumberOfImageFiles < 1) {
            ConfMainWindow.FolderLog.Text += "\nNumberOfImageFiles:" + NumberOfImageFiles + " 0\n";
        } else {
            ConfMainWindow.FolderLog.Text += "\nNumberOfImageFiles:" + NumberOfImageFiles + ":OK.\n";
            return true;
        }
        return false;
    }
    private static bool SortFiles(MainWindow ConfMainWindow, int MaxFile, in string PathName, string[] AllOldFileName) {
        for (int i = MaxFile - 1; i >= 0; --i) {//尻からリネームしないと終わらない?
            FileInfo file = new FileInfo(AllOldFileName[i]);
            while ((file.Name.Length - file.Extension.Length) < 3)
                if (System.IO.File.Exists(PathName + "/0" + file.Name)) {//重複
                    ConfMainWindow.FolderLog.Text += "\n:" + PathName + "/0" + file.Name + ":Exists";
                    MessageBox.Show("Files nama are Duplicate");
                    return false;
                } else
                    file.MoveTo(PathName + "/0" + file.Name);//0->000  1000枚までしか無理 7zは650枚
            if (file.Name[0] == 'z')
                continue;
            if (System.IO.File.Exists(PathName + "/z" + file.Name)) {//重複
                ConfMainWindow.FolderLog.Text += "\n:" + PathName + "/z" + file.Name + ":Exists";
                return false;
            } else
                file.MoveTo(PathName + "/z" + file.Name);//0->000  1000枚までしか無理 7zは650枚
        }
        return true;
    }
    private static void UnSortFiles(in string PathName, string[] AllOldFileName) {
        int i = 0;
        foreach (string f in System.IO.Directory.EnumerateFiles(PathName, "*", System.IO.SearchOption.AllDirectories)) {
            new FileInfo(f).MoveTo(AllOldFileName[i++]);//000->z000
        }/**/
    }
}