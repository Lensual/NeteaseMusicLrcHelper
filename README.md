# NeteaseMusicLrcHelper
Desktop lyrics for NeteaseMusic UWP

网易云音乐UWP桌面歌词助手

<img src=https://raw.githubusercontent.com/Lensual/NeteaseMusicLrcHelper/master/preview.png>

## 实现原理

使用CE抓取内存基址，当前时间、SongID、LRC歌词、翻译版歌词。

使用基址计算偏移，读指针。

通过`当前歌词和下句歌词的时间差`与`当前时间`计算歌词滚动。

## 已知问题 

* 因操作系统环境和`Windows.Media.BackgroundPlayback.exe`文件版本不同，不能全面兼容win10，待适配。

## 待改进

* 皮肤调整
* 设置保存
* 优化歌词同步算法
* x86环境兼容

## 已适配操作系统版本

* Windows 10 Education x64 10.0.17134.137 （文件版本 10.0.17134.1 SHA1 B5A662879F854934CBD65DB995660807EEE6738A）

## 2018.07.04 V0.2

修复改进以下问题

* 歌词同步算法有问题可能会崩溃
* 歌词滚动不精准，是网易云音乐UI进程与播放进程同步慢导致。解决方法 直接读播放进程
* 不在网易云音乐打开歌词看一眼是不会下载LRC的，解决方法 使用SongID和官方API获取LRC
* Windows会挂起后台UWP程序的UI，导致无法实时获取准确的内存信息，并且调用恢复进程API效果不明显
* 从`Windows.Media.BackgroundPlayback`进程获取时间而不是从UI进程