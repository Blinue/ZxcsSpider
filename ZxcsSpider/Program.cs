using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ZxcsSpider.Datas;

namespace ZxcsSpider {
    internal class Program {
        private static readonly HttpClient httpClient = new HttpClient();

        // 数据库 Context
        private static readonly BookDbContext bookDbContext = new BookDbContext();

        // 并发数
        private const int REQUEST_COUNT = 4;

        private static async Task Main(string[] args) {
            if (args.Length != 2 && args.Length != 3) {
                Console.WriteLine("参数错误");
            }

            try {
                int minId = int.Parse(args[0]);
                int maxId = int.Parse(args[1]);
                int delay = 0;
                if (args.Length == 3) {
                    delay = int.Parse(args[2]);
                }

                bookDbContext.Database.Migrate();
                await FetchBooksAsync(minId, maxId, delay);
            } catch (Exception) {
                Console.WriteLine("参数错误");
            }
            /*
            var books = await SelectBooksAsync();
            foreach (var book in books) {
                // 考虑到流量较大，不必并发
                await DownloadBook(book.id, book.title);
            }*/
        }

        // 以评分筛选书籍
        private static async Task<(int id, string title)[]> SelectBooksAsync() {
            var books = await bookDbContext.Books
                .Select(b => new {
                    b.Id,
                    b.Title,
                    Rank = (double)(b.XianCao * 5 + b.LiangCao * 3 - b.KuCao * 1 - b.DuCao * 3) / (b.XianCao + b.LiangCao + b.GanCao + b.KuCao + b.DuCao),
                    RankCount = b.XianCao + b.LiangCao + b.GanCao + b.KuCao + b.DuCao
                })
                .Take(10)
                .ToListAsync();

            return books.Select(b => (b.Id, b.Title))
                .ToArray();
        }

        // 下载书籍
        private static async Task DownloadBook(int id, string title) {
            string url = $"http://www.zxcs.me/download.php?id={id}";
            var response = await httpClient.GetAsync(url);
            if(!response.IsSuccessStatusCode) {
                Console.WriteLine($"下载{id}失败");
                return;
            }

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(await response.Content.ReadAsStringAsync());

            var spanNode = html.DocumentNode
                .Descendants("span")
                .Where(n => n.HasClass("downfile"))
                .First();

            // 获取下载链接
            string downloadUrl = spanNode.Descendants("a")
                .First()
                .GetAttributeValue("href", "");
            if(string.IsNullOrWhiteSpace(downloadUrl)) {
                Console.WriteLine("下载{id}失败");
                return;
            }

            response = await httpClient.GetAsync(downloadUrl);
            if(!response.IsSuccessStatusCode) {
                Console.WriteLine($"下载{id}失败");
                return;
            }

            // 下载至 /books
            string outFile;
            if (downloadUrl.EndsWith(".rar")) {
                outFile = $"books/{title}.rar";
            }else {
                outFile = $"books/{downloadUrl.Substring(downloadUrl.LastIndexOf("/") + 1)}";
            }

            await response.Content.CopyToAsync(File.OpenWrite(outFile));
            Console.WriteLine($"已下载{id}");
        }

        // 批量获取书籍信息并存入数据库
        private static async Task FetchBooksAsync(int minId, int maxId, int delay) {
            if (minId > maxId) {
                return;
            }

            int curId = minId;

            // 并发数为 REQUEST_COUNT
            var tasks = Enumerable.Range(curId, REQUEST_COUNT)
                .Select(id => FetchAndSaveBookAsync(id))
                .ToList();
            curId += REQUEST_COUNT - 1;

            while (tasks.Any()) {
                var task = await Task.WhenAny(tasks);

                if (curId >= maxId) {
                    tasks.Remove(task);
                } else {
                    ++curId;

                    // 等待一定时间，增大获取间隔
                    await Task.Delay(delay);
                    tasks[tasks.FindIndex(t => t == task)] = FetchAndSaveBookAsync(curId);
                }
            }
        }

        // 获取书籍信息和评价并存入数据库
        private static async Task FetchAndSaveBookAsync(int postId) {
            BookInfo bookInfo = await GetBookInfoAsync(postId);
            if (bookInfo == null) {
                Console.WriteLine(postId + " 404");
                return;
            }

            Console.WriteLine($"{bookInfo.Id} {bookInfo.Title} {bookInfo.Author}");

            int[] moods = await GetMoodsAsync(postId);

            using var bookDbContext = new BookDbContext();

            var book = await bookDbContext.Books.FindAsync(postId);
            if (book == null) {
                await bookDbContext.Books.AddAsync(new Book {
                    Id = postId,
                    Author = bookInfo.Author,
                    Title = bookInfo.Title,
                    XianCao = moods[0],
                    LiangCao = moods[1],
                    GanCao = moods[2],
                    KuCao = moods[3],
                    DuCao = moods[4],
                });
            } else {
                book.Author = bookInfo.Author;
                book.Title = bookInfo.Title;
                book.XianCao = moods[0];
                book.LiangCao = moods[1];
                book.GanCao = moods[2];
                book.KuCao = moods[3];
                book.DuCao = moods[4];
            }

            await bookDbContext.SaveChangesAsync();
        }

        // 获取评价
        private static async Task<int[]> GetMoodsAsync(int postId) {
            string url = $"http://www.zxcs.me/content/plugins/cgz_xinqing/cgz_xinqing_action.php?action=show&id={postId}";

            string response = await httpClient.GetStringAsync(url);

            int[] moods = response.Split(',')
                .Select(str => int.Parse(str))
                .ToArray();

            if (moods.Length != 5) {
                throw new Exception();
            }

            return moods;
        }

        // 获取书籍信息
        private static async Task<BookInfo> GetBookInfoAsync(int postId) {
            string url = $"http://www.zxcs.me/post/{postId}";

            var response = await httpClient.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) {
                return null;
            }

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(await response.Content.ReadAsStringAsync());

            string title = html.DocumentNode.Descendants("h1").First().InnerText;
            return new BookInfo(postId, title);
        }
    }
}