﻿# XLRT ConsoleServer

欢迎使用 ConsoleServer ——一个便携化的 Minecraft: Java Edition 服务端部署方案！

## 开始使用

本作者强烈建议你直接从 Release 中直接获取包含特定服务端 ROM 和适配 JDK 或 JRE 的单文件版本，将它安置在任意文件夹内，直接双击启动，无需繁琐配置即刻引导使用！

若你想要的服务端在 Release 中未包含，你也可以将代码克隆下来，只需简单几步，轻松实现制作与分发！

## 制作与分发

该部署器通过 **嵌入的 JDK / JRE 压缩包（Temp.zip）** 和 **嵌入的引导 ROM 镜像（Boot.zip）** 实现旁加载引导，其中引导 ROM 镜像为服务端 .jar 文件修改后缀名或改包得到

> 除非该部署器实在无法旁加载 .jar 修改后缀名的未修改包，否则不建议对包体修改以适配该部署器

> 复制到本地的 ROM 镜像和临时 JDK / JRE 环境将在正常关闭服务端后清理。

若你是针对非大规模分发或只有你的服务器使用，你可以直接使用 VS 的生成功能。若是需要大规模分发或是需要便携化，则需要**通过 Dotnet 生成工具执行单文件发布**。命令如下：

```bash
dotnet publish -r win-x64 -p:PublishSingleFile=true -o <输出路径>
```