// See https://aka.ms/new-console-template for more information
using ICSharpCode.SharpZipLib.Zip;
using System.Text;

namespace BlastZip
{
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
        public int maxThreads = 10;
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
                Log("result: " + password);
                _exitFlag = true;
            }
            return result;
        }

        //测试定长密码
        public bool TestWithLength(int length)
        {
            if (length <= 0) return false;
            if (_exitFlag) return false;
            List<Thread> threads = new List<Thread>();
            int sliceLength = (int)Math.Pow((double)10, (double)length) / maxThreads;
            Console.WriteLine("slice:" + sliceLength);
            for (int i = 0; i < maxThreads; i++)
            {
                if (_exitFlag) return false;
                int min;
                int max;
                if (i == maxThreads - 1)
                {
                    min = sliceLength * i;
                    max = (int)Math.Pow((double)10, (double)length) - 1;
                }
                else
                {
                    min = sliceLength * i;
                    max = sliceLength * (i + 1) - 1;
                }
                Thread thread = new Thread(() =>
                {
                    TestInRange(min, max, length);
                });
                threads.Add(thread);
                thread.Start();
            }
            threads.ForEach((Thread thread) => { thread.Join(); });
            if (_exitFlag) return true;
            else return false;
        }

        public void Run(int length)
        {
            if (_exitFlag) return;
            for (int i = 1; i <= length; i++)
            {
                if (_exitFlag) return;
                TestWithLength(i);
            }
        }

        public void Run(string? _payloadPath)
        {
            if (_exitFlag) return;
            if (File.Exists(_payloadPath))
            {
                string[] payloads = File.ReadAllLines(_payloadPath, Encoding.UTF8);
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
                }
                else Run(8);
            }
        }

        private string PadToLength(string password, int length)
        {
            if (password.Length >= length) return password;
            string ret = "";
            for (int count = 0; count < length - password.Length; count++)
            {
                ret += "0";
            }
            ret += password;
            return ret;
        }

        private bool TestInRange(int min, int max, int length)
        {
            if (_exitFlag) return false;
            if (min > max) return false;
            for (int i = min; i <= max; i++)
            {
                if (_exitFlag) return false;
                bool result = TestPassword(PadToLength(i.ToString(), length));
                if (result) return true;
            }
            return false;
        }

        private void Log(string message)
        {
            File.AppendAllTextAsync(_logPath, message + "\n");
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
}