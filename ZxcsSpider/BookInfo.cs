using System;
using System.Collections.Generic;
using System.Text;

namespace ZxcsSpider {
    class BookInfo {
        public BookInfo(int id, string info) {
            Id = id;

            info = info.Trim();
            if (string.IsNullOrEmpty(info)) {
                return;
            }

            if (!info.StartsWith("《")) {
                Title = info;
                return;
            }

            int p1 = info.IndexOf("》");
            int p2 = info.LastIndexOf("作者：");
            if (p1 == -1 || p2 == -1) {
                Title = info;
                return;
            }

            Title = info[1..p1];
            Author = info.Substring(p2 + "作者：".Length);
        }

        public int Id { get; }

        public string Title { get; } = "";

        public string Author { get; } = "";
    }
}
