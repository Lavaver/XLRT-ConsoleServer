using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace ConsoleServer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "XLRT ConsoleServer";

            Console.WriteLine("XLRT ConsoleServer V1 | <可选的备注>");

            Console.WriteLine("正在释放临时 JDK 环境，请稍候。");

            UnzipJDK();

            await BootServerAsync();
        }



        static void UnzipJDK()
        {
            // 获取当前程序集
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 定义内嵌资源的路径
            string resourceName = "ConsoleServer.Temp.zip";

            // 打开内嵌资源流
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                // 检查资源是否存在
                if (stream == null)
                {
                    Console.WriteLine("内嵌资源不存在。");
                    return;
                }

                // 创建输出目录
                string outputDirectory = "TempJDK";
                Directory.CreateDirectory(outputDirectory);

                // 使用 ZipArchive 打开内嵌资源
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    // 遍历压缩包中的每个条目，并提取到输出目录
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string outputPath = Path.Combine(outputDirectory, entry.FullName);

                        // 如果是文件夹，创建文件夹
                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(outputPath);
                            continue;
                        }

                        // 提取文件
                        using (Stream entryStream = entry.Open())
                        using (Stream outputStream = File.Create(outputPath))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }
            }

            Console.WriteLine("释放临时 JDK 环境完成。");
        }

        static async Task BootServerAsync()
        {
            await Task.Delay(2000);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "ConsoleServer.Boot.zip";
            string targetFilePath = Path.Combine(Environment.CurrentDirectory, "Boot.zip");

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Console.WriteLine("引导 ROM 不存在");
                    return;
                }

                using (FileStream fileStream = File.Create(targetFilePath))
                {
                    await stream.CopyToAsync(fileStream); // 使用异步方式复制内嵌资源内容到目标文件
                }
            }

            Console.WriteLine("正在引导服务端 ROM");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ".\\TempJDK\\jdk-17\\bin\\java.exe",
                Arguments = "-jar Boot.zip",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            // 监听进程的输出流
            process.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 输出流中的每一行日志消息
                    Console.WriteLine(e.Data);

                    // 检查日志消息是否包含指定的关键字
                    if (e.Data.Contains("ThreadedAnvilChunkStorage: All dimensions are saved"))
                    {
                        // 如果日志消息包含关键字，则执行清理操作
                        await CreateDelBat();
                        StartDel();
                        Environment.Exit(0);
                    }
                }
            };

            process.Start();

            process.BeginOutputReadLine();

            string input;
            while ((input = Console.ReadLine()) != null)
            {
                process.StandardInput.WriteLine(input);
            }
        }

        static async Task CreateDelBat()
        {
            // 定义要执行的批处理命令
            string[] batchCommands = new string[]
            {
                "@echo off",
                "title XLRT ConsoleServer Cleanup Batch",
                "echo Cleaning temporary files in progress. Please do not close the window during the entire cleaning process! The window will be safely closed automatically upon completion.",
                "ping 127.0.0.1 -n 10 > nul",
                "rd /s /q \"./TempJDK\"",
                "ping 127.0.0.1 -n 1 > nul",
                "del /q \"Boot.zip\"",
                "ping 127.0.0.1 -n 1 > nul",
                "del /q \"cleanup.bat\""
            };

            // 设置批处理文件的路径
            string batchFilePath = "cleanup.bat";

            try
            {
                // 创建并写入批处理文件
                using (StreamWriter sw = new StreamWriter(batchFilePath))
                {
                    foreach (string command in batchCommands)
                    {
                        await sw.WriteLineAsync(command);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"出错：{ex.Message}");
            }
        }

        static void StartDel()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cleanup.bat",
                UseShellExecute = true
            });
        }

    }


}
