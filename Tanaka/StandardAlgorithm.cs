using System.IO;
public class StandardAlgorithm {
    public static void ExecuteAnotherApp(in string FileName, in string Arguments, bool UseShellExecute, bool CreateNoWindow)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
            FileName = FileName,
            Arguments = Arguments,
            UseShellExecute = UseShellExecute,
            CreateNoWindow = CreateNoWindow    // コンソール・ウィンドウを開かない
        }).WaitForExit();
    }
    public class Sort {
        public static void Bubble(byte[] numbers) {
            for (int i = 0; i < (numbers.Length - 1); i++)
                for (int j = (numbers.Length - 1); j > i; j--)
                    if (numbers[j - 1] > numbers[j]) {
                        byte temp = numbers[j - 1];
                        numbers[j - 1] = numbers[j];
                        numbers[j] = temp;
                    }
        }
    }
    public class Math {
        public static int MakeItOdd(int Integer) {
            return (Integer - ((Integer + 1) & 1));//奇数にしたい -1は奇数
        }
        public static int MakeItEven(int Integer) {
            return (Integer - (Integer & 1));//偶数にしたい
        }
    }
    public class Directory {
        /// フォルダのサイズを取得する
        // </summary>
        // <param name="dirInfo">サイズを取得するフォルダ</param>
        // <returns>フォルダのサイズ（バイト）</returns>
        public static long GetDirectorySize(DirectoryInfo dirInfo) {
            long size = 0;
            foreach (FileInfo fi in dirInfo.GetFiles())//フォルダ内の全ファイルの合計サイズを計算する
                size += fi.Length;
            //foreach (DirectoryInfo di in dirInfo.GetDirectories())//サブフォルダのサイズを合計していく
            //    size += GetDirectorySize(di);
            return size;
        }
        public string[] GetFolderName(string Folder_Name) {// 指定フォルダの直下にある全てのフォルダ名を取得するメソッド// 第１引数Folder_Name: 対象のフォルダ
            string[] Folder_List; // フォルダ名格納用配列
            if (System.IO.Directory.Exists(Folder_Name) == false) {// 指定フォルダが無い場合
                Folder_List = new string[0]; // 要素数が0の配列を返却
                return Folder_List;
            }
            Folder_List = System.IO.Directory.GetDirectories(Folder_Name);// フォルダ名取得
            return Folder_List;// 戻り値: 取得したフォルダ名の配列
        }
        public string[] GetFileName(string Folder_Name) {// 指定フォルダの直下にある全てのファイル名を取得するメソッド// 第１引数Folder_Name: 対象のフォルダ
            string[] File_List; // ファイル名格納用配列
            if (System.IO.Directory.Exists(Folder_Name) == false) { // 指定フォルダが無い場合
                File_List = new string[0]; // 要素数が0の配列を返却
                return File_List;
            }
            File_List = System.IO.Directory.GetFiles(Folder_Name);// ファイル名取得
            return File_List;// 戻り値: 取得したファイル名の配列
        }
        // CopyFolderフォルダ複写用メソッド　内部フォルダ構造までコピーできる
        // 第１引数Source_Folder_Name: 複写元フォルダ
        // 第２引数Dest_Folder_Name: 複写先フォルダ
        // 第３引数Overwrite_Flag: 複写先に既に同名ファイルが存在する場合の指示。 true:上書きする / false: 上書きしない
        public void CopyFolder(string Source_Folder_Name, string Dest_Folder_Name, bool Overwrite_Flag) {
            if (!System.IO.Directory.Exists(Dest_Folder_Name)) {// 複写先フォルダが存在しない場合、フォルダを作成
                System.IO.Directory.CreateDirectory(Dest_Folder_Name);// フォルダ作成
                System.IO.File.SetAttributes(Dest_Folder_Name, System.IO.File.GetAttributes(Source_Folder_Name));// 作成したフォルダに、フォルダの属性を複写
                Overwrite_Flag = true; // 複写先フォルダを作ったのだから、そこにファイルは存在しないので、以後上書きで可
            }
            string[] File_List = GetFileName(Source_Folder_Name);// 複写元フォルダ直下のファイル名を取得
            if (Overwrite_Flag) {// 複写先に既に同名ファイルが存在する場合に、上書きが許可されている場合
                foreach (string File_Work in File_List) {// 複写元フォルダの直下にあるファイルを複写
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    System.IO.File.Copy(File_Work, Dest_Direc_Work, true);// ファイル複写
                }
            } else {// 複写元フォルダの直下にあるファイルを複写
                foreach (string File_Work in File_List) {
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (!System.IO.File.Exists(Dest_Direc_Work)) {
                        System.IO.File.Copy(File_Work, Dest_Direc_Work, false); // ファイル複写
                    }
                }
            }
            //以下再帰的に内部フォルダ構造までコピーしてしまう
            string[] Folder_List = GetFolderName(Source_Folder_Name);// 複写元フォルダ直下のフォルダ名を取得
            foreach (string Directory_Work in Folder_List) {// 複写元フォルダの直下にある全フォルダに対して、フォルダ複写用メソッド(自メソッド)を実行
                string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(Directory_Work));// 複写先フォルダ名の末尾に、複写元フォルダ名を付加
                CopyFolder(Directory_Work, Dest_Direc_Work, Overwrite_Flag);// 指定フォルダ直下の各フォルダにおいて、複写処理を実行
            }
        }
        // MoveAllFileFolderファイル複写用メソッド　指定フォルダ内のファイルをすべて移動。階層下のは無視。
        // 第１引数Source_Folder_Name: 複写元フォルダ
        // 第２引数Dest_Folder_Name: 複写先フォルダ
        // 第３引数Overwrite_Flag: 複写先に既に同名ファイルが存在する場合の指示。 true:上書きする / false: 上書きしない
        public void MoveAllFileFolder(string Source_Folder_Name, string Dest_Folder_Name, bool Overwrite_Flag) {
            if (!System.IO.Directory.Exists(Dest_Folder_Name)) {// 複写先フォルダが存在しない場合、フォルダを作成
                System.IO.Directory.CreateDirectory(Dest_Folder_Name);// フォルダ作成
                System.IO.File.SetAttributes(Dest_Folder_Name, System.IO.File.GetAttributes(Source_Folder_Name));// 作成したフォルダに、フォルダの属性を複写
                Overwrite_Flag = true; // 複写先フォルダを作ったのだから、そこにファイルは存在しないので、以後上書きで可
            }
            string[] File_List = GetFileName(Source_Folder_Name);// 複写元フォルダ直下のファイル名を取得
            if (Overwrite_Flag) {// 複写先に既に同名ファイルが存在する場合に、上書きが許可されている場合
                foreach (string File_Work in File_List) {// 複写元フォルダの直下にあるファイルを複写
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (System.IO.File.Exists(Dest_Direc_Work)) {//あるか確認
                        System.IO.File.Delete(Dest_Direc_Work);//あったら消す
                    }
                    System.IO.File.Move(File_Work, Dest_Direc_Work);// ファイル移動
                }
            } else {// 複写元フォルダの直下にあるファイルを複写
                foreach (string File_Work in File_List) {
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (!System.IO.File.Exists(Dest_Direc_Work)) {//あるか確認
                        System.IO.File.Move(File_Work, Dest_Direc_Work); //ないならファイル移動
                    }
                }
            }
        }
    }
}