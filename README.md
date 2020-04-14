# ZxcsSpider

知轩藏书（http://www.zxcs.me）爬虫

爬取书籍存入本地数据库，也可以筛选和下载。

可自定义并发数。

## 如何使用

命令行格式为：
ZxcsSpider.exe minId maxId delay

* minId：最小的书籍ID
* maxId：最大的书籍ID
* delay：延时，默认为0，指定此值以降低请求频率

## .NET Core的优势

* .NET Core的Entity Framwork Core可轻松与数据库（sqlite）交互，包括创建、修改、查询
* .NET 使用的 async/await 并发模型十分简洁且高效。通过消息循环，所有并发都是在一个线程上完成的。这在其他语言中几乎不可能实现。
