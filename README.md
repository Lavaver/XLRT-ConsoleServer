﻿# XLRT ConsoleServer

欢迎使用 ConsoleServer ——一个便携化的 Minecraft: Java Edition 服务端部署方案！

## 开始使用

本作者强烈建议你直接从 Release 中直接获取包含特定服务端 ROM 和适配 JDK 或 JRE 的单文件版本，将它安置在任意文件夹内，直接双击启动，无需繁琐配置即刻引导使用！

若你想要的服务端在 Release 中未包含，你也可以将代码克隆下来，只需简单几步，轻松实现制作与分发！

> 注：1.12.2~1.20.1 的纯控制台版本已停止维护。并且最近本作者发现我的基友使用我编写的开源代码制作闭源部署端，现**郑重警告我的基友请你立刻停止闭源行为！该代码及其软件副本遵循的不是 MIT 而是 GPLv2 协议！按照开源许可证要求必须开源！该许可证协议是被法律认可且具有法律效力的！你不能闭源！**

## 制作与分发

该部署器通过 **嵌入的 JDK / JRE 压缩包（Temp.zip）** 和 **嵌入的引导 ROM 镜像（Boot.zip）** 实现旁加载引导，其中引导 ROM 镜像为服务端 .jar 文件修改后缀名或改包得到

> 除非该部署器实在无法旁加载 .jar 修改后缀名的未修改包，否则不建议对包体修改以适配该部署器

> 复制到本地的 ROM 镜像和临时 JDK / JRE 环境将在正常关闭服务端后清理。

若你是针对非大规模分发或只有你的服务器使用，你可以直接使用 VS 的生成功能。若是需要大规模分发或是需要便携化，则需要**通过 Dotnet 生成工具执行单文件发布**。命令如下：

```bash
dotnet publish -r win-x64 -p:PublishSingleFile=true -o <输出路径>
```

## 启动流程

无论是新手还是大佬，你在这里的起点都是这里————启动加载器

启动后，加载器会自动进行释放操作，将本次运行需要用到的临时 JRE / JDK 及临时 ROM 复制到加载器存放目录下

当释放操作完成后，接下来将调用 JRE / JDK 使用特殊方式将 ROM 侧载到 JVM 中，若你的设备性能不错，引导过程将在瞬间完成，之后的控制权转交给 ROM

当 ROM 正常结束运行后，软件将退出并进入 `XLRT ConsoleServer Cleanup Batch` 批处理环境下，执行最后的清理临时文件操作，保证下次启动是干净的避免错误发生

## 服务端与部署端的区别

- 服务端指使用纯 java 环境手动运行服务端 .jar 文件（如原版端、Mohist 端、Spigot 端等），所有的配置需要完全由你自己修改，这种端的好处是通用性强，缺点是对于新手腐竹来说特别困难

- 部署端指通过搭建临时 JRE / JDK 环境 + ROM 侧载 + 自动初始化配置从而迅速且高效的部署一个服务端，这种端的好处是易用、容易部署、安全性高，缺点是通用性较差

## 最近更改

- 将自动签署 eula 更改为全局性的，不再针对单独版本单独开小灶
- JRE / JDK 临时释放文件夹改为 GUID 格式名称
- 释放后的临时 `Boot.zip` 镜像将去除扩展名，改为使用 `.{GUIDBoot}` 格式，在制作时无需更改原本的嵌入式资源镜像，仅在释放时构建临时唯一标识符 ROM 镜像
- 取消部分版本的 `nogui` 参数，将按照常规模式运行
- 添加对初次启动及之后启动的端口占用状态检查，如被占用，则要求修改端口（初次启动时若端口占用按引导修改后会自动生成新的服务端配置文件）