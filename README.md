# BlastZip

## 使用说明:

BlastZip <-f ZipPath> [OutputPath] [-t MaxLength | -q | -p PayloadPath] [--log LogPath]

运行完毕后在log文件中寻找"result: password"即为爆破的结果

-t 为指定最大长度的爆破

-q 为最大长度为6的爆破

-p 为指定字典的爆破

若不指定则进行最大长度为8的爆破

**无字典情况下只爆破纯数字密码**

## 运行过程说明:

原理是使用密码尝试解压，如果解压成功就写入到log文件中，解压失败就继续尝试

目前部分密码尝试时可能出现不明原因报错，大部分是由于密码错误，但是尚未经过一一确认，故将产生报错时尝试的密码一并写入log文件，为"error: password"

目前使用10个线程同时进行爆破
