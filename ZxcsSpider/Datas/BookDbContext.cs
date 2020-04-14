using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;


namespace ZxcsSpider.Datas {
    // 使用 sqlite
    class BookDbContext: DbContext {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlite(@"Data Source=book.db");
        }

        public DbSet<Book> Books { get; set; }
    }
}
