using System.Diagnostics;
using System.IO.Compression;
using System.Net.Sockets;
using System.Reflection;


namespace ConsoleServer
{
    public class Program
    {

        // 定义静态成员变量
        private static string GUIDJava;

        private static string GUIDBoot;

        /// <summary>
        /// 主逻辑
        /// </summary>
        /// <returns>部署信息</returns>
        static async Task Main(string[] args)
        {
            Console.Title = "XLRT ConsoleServer";

            Console.WriteLine("XLRT ConsoleServer");

            // 检查是否存在配置文件
            string configFile = "server.properties";
            int defaultPort = 25565;

            int port = defaultPort;

            if (File.Exists(configFile))
            {
                // 读取配置文件中的端口号
                string[] lines = await File.ReadAllLinesAsync(configFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("server-port="))
                    {
                        if (int.TryParse(line.Substring("server-port=".Length), out int serverPort))
                        {
                            port = serverPort;
                            while (await CheckPortInUseAsync(port))
                            {
                                Console.WriteLine($"端口 {port} 已被占用，你需要输入其他端口号并按 Enter 继续：");
                                string input = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(input))
                                {
                                    if (int.TryParse(input, out int newPort))
                                    {
                                        port = newPort;
                                    }
                                    else
                                    {
                                        Console.WriteLine("无效的端口号，请重新输入。");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 检查端口是否被占用
            while (await CheckPortInUseAsync(port))
            {
                Console.WriteLine($"端口 {port} 已被占用，你需要输入其他端口号并按 Enter 继续：");
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    if (int.TryParse(input, out int newPort))
                    {
                        port = newPort;
                    }
                    else
                    {
                        Console.WriteLine("无效的端口号，请重新输入。");
                    }
                }
            }

            // 修改配置文件中的端口号
            if (File.Exists(configFile))
            {
                // 要修改的属性名和新的属性值
                string propertyName = "server-port";
                int newValue = port; 

                ModifyPropertyInConfig(configFile, propertyName, newValue);
            }
            else
            {
                await InitServerProperties(port);
            }

            Console.WriteLine("端口已设置完成。");

            Console.WriteLine("正在初始化临时环境并引导服务端");

            UnzipJDK();

            await CheckEula();

            await BootServerAsync();
        }

        static void ModifyPropertyInConfig(string configFile, string propertyName, int newValue)
        {

            // 读取配置文件的所有行
            string[] lines = File.ReadAllLines(configFile);

            // 遍历每一行，查找要修改的属性名
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(propertyName + "="))
                {
                    // 找到属性名所在的行，修改其值
                    lines[i] = propertyName + "=" + newValue;
                    break; // 找到后停止搜索
                }
            }

            // 将修改后的内容写回配置文件
            File.WriteAllLines(configFile, lines);
        }


        static async Task<bool> CheckPortInUseAsync(int port)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync("127.0.0.1", port);
                    // 连接成功，表示端口已被占用
                    Console.WriteLine($"端口 {port} 被占用。");
                    return true;
                }
                catch (SocketException)
                {
                    // 连接失败，表示端口未被占用
                    Console.WriteLine($"端口 {port} 未被占用。");
                    return false;
                }
            }
        }

        static async Task InitServerProperties(int port)
        {
            // 初始化 server.properties 文件为指定内容
            string serverPropertiesContent = $@"
allow-flight=true
allow-nether=true
broadcast-console-to-ops=true
broadcast-rcon-to-ops=true
debug=false
difficulty=easy
enable-command-block=false
enable-jmx-monitoring=false
enable-query=false
enable-rcon=false
enable-status=true
enforce-secure-profile=true
enforce-whitelist=false
entity-broadcast-range-percentage=100
force-gamemode=false
function-permission-level=2
gamemode=creative
generate-structures=true
generator-settings={{}}
hardcore=false
hide-online-players=false
initial-disabled-packs=
initial-enabled-packs=vanilla
level-name=world
level-seed=
level-type=minecraft\\:normal
max-chained-neighbor-updates=1000000
max-players=20
max-tick-time=60000
max-world-size=29999984
motd=A Minecraft Server (Pre-Spawn Config)
network-compression-threshold=512
online-mode=true
op-permission-level=4
player-idle-timeout=0
prevent-proxy-connections=false
pvp=false
query.port=25565
rate-limit=0
rcon.password=
rcon.port=25575
require-resource-pack=false
resource-pack=
resource-pack-prompt=
resource-pack-sha1=
server-ip=
server-port={port}
simulation-distance=10
spawn-animals=true
spawn-monsters=true
spawn-npcs=true
spawn-protection=16
sync-chunk-writes=true
text-filtering-config=
use-native-transport=true
view-distance=12
white-list=false";

            // 将内容写入 server.properties 文件
            await System.IO.File.WriteAllTextAsync("server.properties", serverPropertiesContent);
        }



        /// <summary>
        /// 搭建临时 JDK / JRE 环境逻辑
        /// </summary>
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

                GUIDJava = Guid.NewGuid().ToString();

                // 创建输出目录
                string outputDirectory = GUIDJava;
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

            Console.WriteLine("释放临时 Java 环境完成。");
        }

        /// <summary>
        /// 自动检查 eula 并签署逻辑
        /// </summary>
        /// <returns>eula.txt 文件，内部已包含自动签署信息</returns>
        static async Task CheckEula()
        {
            string eulaFilePath = "eula.txt";

            // 检查是否存在eula.txt文件
            if (File.Exists(eulaFilePath))
            {
                Console.WriteLine("eula.txt 文件已存在。跳过");
            }
            else
            {
                // 如果文件不存在，则创建一个新的eula.txt文件并写入内容
                try
                {
                    using (StreamWriter writer = new StreamWriter(eulaFilePath))
                    {
                        await writer.WriteLineAsync("eula=true");
                    }
                    Console.WriteLine("已为你自动同意 Minecraft Eula ，请您在使用本加载器时注意遵守 Eula 条款内容！");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建 eula.txt 文件时发生错误：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 释放 ROM 并引导它
        /// </summary>
        /// <returns>控制台输出</returns>
        static async Task BootServerAsync()
        {
            GUIDBoot = Guid.NewGuid().ToString();
            await Task.Delay(2000);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "ConsoleServer.Boot.zip";
            string targetFilePath = Path.Combine(Environment.CurrentDirectory, $".{GUIDBoot}");

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

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $".\\{GUIDJava}\\bin\\java.exe",
                Arguments = $"-jar \".{GUIDBoot}\"",
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
                    if (e.Data.Contains("Saving chunks for level 'world'/overworld")) // 针对于较早版本
                    {
                        // 如果日志消息包含关键字，则执行清理操作

                        await CreateDelBat();
                        StartDel();
                        Environment.Exit(0);
                    }
                    else if (e.Data.Contains("ThreadedAnvilChunkStorage: All dimensions are saved")) // 针对于自 1.16.5 以后的新版
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

            await CreateDelBat();
            StartDel();
            Environment.Exit(0);

        }

        /// <summary>
        /// 构建清理脚本程序
        /// </summary>
        /// <returns>清理脚本（cleanup.bat）</returns>
        static async Task CreateDelBat()
        {
            // 定义要执行的批处理命令
            string[] batchCommands = new string[]
            {
                "@echo off",
                "title XLRT ConsoleServer Cleanup Batch",
                "echo Cleaning temporary files in progress. Please do not close the window during the entire cleaning process! The window will be safely closed automatically upon completion.",
                "ping 127.0.0.1 -n 10 > nul",
                $"rd /s /q \"./{GUIDJava}\"",
                "ping 127.0.0.1 -n 1 > nul",
                $"del /q \".{GUIDBoot}\"",
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

        /// <summary>
        /// 执行清理脚本程序
        /// </summary>
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
