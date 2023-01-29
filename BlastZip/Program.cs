// See https://aka.ms/new-console-template for more information
using ICSharpCode.SharpZipLib.Zip;
using System.Text;

class Progaram
{
    private static BlastZip? blastZip;

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Use command \"BlastZip <-f ZipPath> [OutputPath] [-t MaxLength | -q | -p PayloadPath] [--log LogPath]\" to find the password of a zip file");
            return;
        }
        if (args[0] == "-f")
        {
            //输出路径
            string outputpath;
            if ((args.Length - 1 > 2) && Directory.Exists(args[2])) outputpath = args[2];
            else
            {
                string? directory = Path.GetDirectoryName(args[1]);
                if (directory != null) outputpath = directory;
                else outputpath = System.Environment.CurrentDirectory;
            }
            //日志
            if (args[args.Length - 2] == "--log" && Directory.Exists(args[args.Length - 1]))
                blastZip = new BlastZip(args[1], outputpath, args[args.Length - 1]);
            else
                blastZip = new BlastZip(args[1], outputpath, "BlastZipLog.txt");
        }
        if (blastZip != null)
        {
            DateTime time1 = DateTime.UtcNow;
            //指定密码长度爆破
            if (args.Contains("-t"))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args.Length - 1 > i && args[i] == "-t")
                    {
                        blastZip.Run(int.Parse(args[i + 1]));
                        break;
                    }
                    else if (i == args.Length - 1)
                    {
                        blastZip.Run(8);
                    }
                }
            }
            else
            {
                //使用字典爆破
                if (args.Contains("-p"))
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args.Length - 1 > i && args[i] == "-p")
                        {
                            blastZip.Run(args[i + 1]);
                            break;
                        }
                        else if (i == args.Length - 1)
                        {
                            blastZip.Run(8);
                        }
                    }
                }
                //快速爆破
                else if (args.Contains("-q")) blastZip.Run(6);
                //默认长度爆破
                else blastZip.Run(8);
            }
            DateTime time2 = DateTime.UtcNow;
            Console.WriteLine(time2 - time1);
        }
    }
}

class BlastZip
{
    private string _filePath;
    private string _outputPath;
    private string _logPath;
    private bool _exitFlag = false;
    private ZipFile? _zipFile;

    public BlastZip(string filePath, string outputPath, string logPath)
    {
        this._filePath = filePath;
        this._outputPath = outputPath;
        this._logPath = logPath;
        File.WriteAllText(logPath, "");
        Unzip.logPath = _logPath;
        if (File.Exists(filePath))
        {
            Stream stream = File.OpenRead(filePath);
            try
            {
                _zipFile = new ZipFile(stream);
                foreach (ZipEntry entry in _zipFile)
                {
                    Console.WriteLine(entry.Name);
                }
            }
            catch (ZipException)
            {
                Console.WriteLine("Not A Zip File");
            }
        }
        else
        {
            throw new FileNotFoundException();
        }
        _logPath = logPath;
    }
    //测试单个密码
    public bool TestPassword(string password)
    {
        Console.WriteLine(password);
        bool result = Unzip.UnzipFile(_filePath, _outputPath, password);
        if (result)
        {
            Console.WriteLine("Success");
            File.AppendAllText(_logPath, "result:" + password + "\n");
            _exitFlag = true;
        }        
        return result;
    }
    //测试定长密码
    public bool TestWithLength(int length)
    {
        if (length <= 0) return false;
        for (int i = 0; i < (int)Math.Pow((double)10, (double)length); i++)
        {
            if (_exitFlag) return false;
            string password = "";
            for (int count = 0; count < length - i.ToString().Length; count++)
            {
                password += "0";
            }
            password += i.ToString();
            bool result = TestPassword(password);
            if (result) return true;
        }
        return false;
    }

    public void Run(int length)
    {
        if (_exitFlag) return;
        for (int i = 1; i <= length; i++)
        {
            TestWithLength(i);
        }
    }

    public void Run(string? _payloadPath)
    {
        if (_exitFlag) return;
        if (File.Exists(_payloadPath))
        {
            string[] payloads = File.ReadAllLines(_payloadPath,Encoding.UTF8);
            foreach (string payload in payloads) TestPassword(payload);
        }
        else
        {
            if (Directory.Exists(_payloadPath))
            {
                DirectoryInfo di = new DirectoryInfo(_payloadPath);
                foreach (DirectoryInfo directory in di.GetDirectories())
                {
                    Run(directory.FullName);
                }
                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Extension == ".txt") Run(file.FullName);
                }
                
                /*foreach (DirectoryInfo directory in di.GetDirectories())
                    Run(directory.FullName);*/
            }
            else Run(8);
        }
    }
}
class Unzip
{
    public static string? logPath;
    public static bool UnzipFile(string _filePath, string _outputPath, string? _password = null)
    {
        if (string.IsNullOrEmpty(_filePath) || string.IsNullOrEmpty(_outputPath))
            return false;
        Stream _inputSteam = File.OpenRead(_filePath);
        return UnzipFile(_inputSteam, _outputPath, _password);
    }
    public static bool UnzipFile(Stream _inputStream, string _outputPath, string? _password = null)
    {
        if ((null == _inputStream) || string.IsNullOrEmpty(_outputPath))
            return false;

        // 创建文件目录
        if (!Directory.Exists(_outputPath))
            Directory.CreateDirectory(_outputPath);

        // 解压Zip包
        ZipInputStream zipInputStream = new ZipInputStream(_inputStream);

        return UnzipFile(zipInputStream, _outputPath, _password);
    }
    public static bool UnzipFile(ZipInputStream _inputStream, string _outputPath, string? _password = null)
    {
        if (!string.IsNullOrEmpty(_password))
            _inputStream.Password = _password;
        ZipEntry? entry = null;
        while (null != (entry = _inputStream.GetNextEntry()))
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            string filePathName = Path.Combine(_outputPath, entry.Name);
            // 创建文件目录
            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(filePathName);
                continue;
            }

            // 写入文件
            try
            {
                using (FileStream fileStream = new FileStream(filePathName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    byte[] bytes = new byte[1024];

                    while (true)
                    {
                        int count = _inputStream.Read(bytes, 0, bytes.Length);

                        if (count > 0)
                            fileStream.Write(bytes, 0, count);
                        else break;
                    }
                }
            }
            catch (ZipException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return UnzipFile(_inputStream, _outputPath, _password);
            }
            catch (System.Exception _e)
            {
                if (logPath != null)
                    File.AppendAllTextAsync(logPath, "error:" + _password + "\n");
                Console.WriteLine("[ZipUtility.UnzipFile]: " + _e.ToString());
                return false;
            }
        }
        return true;
    }
}
