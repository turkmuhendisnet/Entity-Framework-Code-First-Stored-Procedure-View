using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StoredProcedure.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            DatabaseContext db = new DatabaseContext();

            db.Books.ToList();
            db.ExecuteInsertFakeDataSp();
            var result = db.ExecuteGetBooksGroupByPublishedDateSP(1999, 2010);
            var result2 = db.GetBookInfo();

            return View();
        }
    }

    public class DatabaseContext : DbContext
    {

        public DbSet<Book> Books { get; set; }

        public DatabaseContext()
        {
            //DATA BASE oluşturulurken şunarı yap
            Database.SetInitializer(new DbInitializer());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Book>().MapToStoredProcedures(Config =>
            {
                Config.Insert(i => i.HasName("BooksInsertSP"));
                Config.Update(u =>
                {
                    u.HasName("BooksInsertSP");
                    u.Parameter(p => p.Id, "BookId");
                });
                Config.Delete(d => d.HasName("BooksDeleteSP"));

            });
        }

        public void ExecuteInsertFakeDataSp()
        {
            Database.ExecuteSqlCommand("EXEC InsertFakeDataSp");
        }

        public List<BooksGroupByPublishedDate> ExecuteGetBooksGroupByPublishedDateSP(int StartYear,int StopYear)
        {
            return
           Database.SqlQuery<BooksGroupByPublishedDate>("EXEC GetBooksGroupByPublishedDateSP @p0,@p1", StartYear, StopYear).ToList();
        }

        public  List<BookInfo>GetBookInfo()
        {
            return
                Database.SqlQuery<BookInfo>("select *from GetBooksInfoVW").ToList();
        }
    }


    // DbInitializer oluşturuyorum // Ne zaman çalışacağını ve hangi database ile çalışacağını söylüyorum "CreateDatabaseIfNotExists<DatabaseContext>" eğer data base yoksa oluştur
    public class DbInitializer : CreateDatabaseIfNotExists<DatabaseContext>
    {
        protected override void Seed(DatabaseContext context) 
        {
            //context.Database.ExecuteSqlCommand("select * from Books where Id=@p0 and Id=@p1", 5, 6);

            context.Database.ExecuteSqlCommand(
                 @"create procedure InsertFakeDataSp
                    as
                    begin

                    insert into dbo.Books(Name, Description, PublishedDate) values('Da  Vinci Code', 'Da Vinci Şifresi', '2003-02-01')
                    insert into dbo.Books(Name, Description, PublishedDate) values('Angels & Demons', 'Melekler ve Şeytanlar', '200-03-30')
                    insert into dbo.Books(Name, Description, PublishedDate) values('Lost Symbol', 'Kayıp Sembol', '2009-01-29')

                    end"
                );

            context.Database.ExecuteSqlCommand(
                 @"create procedure GetBooksGroupByPublishedDateSP
                    @p0 int,
                    @p1 int
                    as
                    begin
	                    select TBL.PublishedDate,COUNT(TBL.PublishedDate) as [Count]
	                    from(
		                    select year (PublishedDate) as PublishedDate
		                    from Books
		                    where YEAR(PublishedDate) between @p0 and @p1
	                    ) as TBL
	                    GROUP BY TBL.PublishedDate
	
                    end"
                  );

            context.Database.ExecuteSqlCommand(
                @"CREATE view [dbo].[GetBooksInfoVW]
                    as
                    select
	                    Id,
	                    Name+' : '+Description +'('+ CONVERT(nvarchar(20),PublishedDate)+')' as Info
                    from dbo.Books"
                    );
        }
    }

    public class BooksGroupByPublishedDate
    {
        public int PublishedDate { get; set; }
        public int Count { get; set; }
    }

    public class BookInfo
    {
        public int Id { get; set; }
        public string Info { get; set; }
    }

    [Table("Books")]
    public class Book
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime PublishedDate { get; set; }
    }



}