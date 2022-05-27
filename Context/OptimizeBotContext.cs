using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OptimizeBot.Model;
using Newtonsoft.Json;

namespace OptimizeBot.Context
{
    public class OptimizeBotContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
            .UseSqlite("Data Source=optimizebot.db")
            /*.UseLazyLoadingProxies()
            .EnableSensitiveDataLogging(true)*/;

        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //---------------------- User ----------------------------

            modelBuilder.Entity<User>()
                .Property(u => u.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()")
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Session)
                .WithOne(u => u.User)
                .HasForeignKey<Session>(s => s.UserId)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Navigation(u => u.Session)
                .AutoInclude();

            //---------------------- Session ----------------------------
            modelBuilder.Entity<Session>()
                .Property(s => s.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()")
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw); 

           //---------------------- Provider ----------------------------

           modelBuilder.Entity<Provider>()
                .Property(p => p.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()")
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

            modelBuilder.Entity<Provider>()
                .HasOne(p => p.Config)
                .WithOne(c => c.Provider)
                .HasForeignKey<ProviderConfig>(p => p.ProviderId)
                .IsRequired();

            modelBuilder.Entity<Provider>().Navigation(p => p.Catalogs).AutoInclude();
            modelBuilder.Entity<Provider>().Navigation(p => p.Config).AutoInclude();

            var utilitiesOrange = new[]
            {
                new Utility("11", "Camwater Postpaid"),
                new Utility("13", "Eneo Postpaid"),
                new Utility("6", "Orange")
            };

            modelBuilder.Entity<Provider>()
                .HasData(
                    new Provider
                    {
                        ProviderId = 1,
                        Name = "Orange Money",
                        UtilityProviders = JsonConvert.SerializeObject(utilitiesOrange)
                    },
                    new Provider
                    {
                        ProviderId = 2,
                        Name = "MTN Mobile Money"
                    });

            //---------------------- Config ----------------------------
            modelBuilder.Entity<ProviderConfig>()
                .HasData(
                    new ProviderConfig
                    {
                        Id = 1,
                        ProviderId = 1,
                        BaseServiceUssd = @"#150*",
                        ReceiptApiUrl = @"http://mafacture.orange.cm/omoney/api/impressions/generikprinter?service_id={0}&txnid={1}&contenttype=json",
                        ReceiptApiHost = "mafacture.orange.cm",
                        ReceiptApiReferer = @"http://mafacture.orange.cm/omoney/",
                        TrxIdLength = 20
                    },
                    new ProviderConfig
                    {
                        Id = 2,
                        ProviderId = 2,
                        BaseServiceUssd = @"*126*"
                    }); ;

            //---------------------- Service ----------------------------

            modelBuilder.Entity<Service>()
               .Property(s => s.CreationDate)
               .ValueGeneratedOnAdd()
               .HasDefaultValueSql("datetime()")
               .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Parent)
                .WithMany(s => s.Services)
                .HasForeignKey(s => s.ParentId)
                .IsRequired(false);

            modelBuilder.Entity<Service>()
                .Navigation(s => s.Catalogs)
                .AutoInclude();

            modelBuilder.Entity<Service>()
                .HasData(
                    new Service
                    {
                        ServiceId = 1,
                        Command = "/start",
                        EnDesc = "Home menu",
                        FrDesc = "Accueil",
                        ParentId = null
                    },
                    new Service
                    {
                        ServiceId = 11,
                        Command = "/cashout",
                        EnDesc = "Cash-Out",
                        FrDesc = @"Retrait d'espèces",
                        ParentId = 1
                    },
                    new Service
                    {
                        ServiceId = 12,
                        Command = "/cashin",
                        EnDesc = "Cash-In",
                        FrDesc = @"Dépôt d'espèces",
                        ParentId = 1
                    },
                    new Service
                    {
                        ServiceId = 13,
                        Command = "/receipt",
                        EnDesc = "Receipt download",
                        FrDesc = "Télecharg. de reçu",
                        ParentId = 1
                    },
                    new Service
                    {
                        ServiceId = 99,
                        Command = "/about",
                        EnDesc = "Who are we?",
                        FrDesc = "Qui sommes-nous?",
                        ParentId = 1
                    });

            //---------------------- Pricing ---------------------------------------------------------------

            modelBuilder.Entity<Pricing>()
                .Property(pricing => pricing.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()")
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

            modelBuilder.Entity<Pricing>()
                .HasOne(p => p.Catalogs)
                .WithOne(ps => ps.Pricing)
                .HasForeignKey<Catalog>(ps => ps.PricingId)
                .IsRequired(false);

            var linesCashOutOrange = new[]
            {
                new Line(From:500, To:2_500, Fee:50),
                new Line(From:2_501, To:175_000, Fee:0.02),
                new Line(From:175_001, To:505_000, Fee:3_500)
            };
            var linesCashInOrange = new[]
            {
                new Line(From:50, To:100_000, Fee:0.005),
                new Line(From:100_001, To:1_000_000, Fee:500)
            };
            var linesCashOutMTN = new[]
            {
                new Line(From:100, To:2_500, Fee:50),
                new Line(From:2_501, To:175_000, Fee:0.02),
                new Line(From:175_001, To:500_000, Fee:3_500)
            };
            var linesCashInMTN = new[]
            {
                new Line(From:1, To:100_000, Fee:0.005),
                new Line(From:100_001, To:1_000_000, Fee:500)
            };

            modelBuilder.Entity<Pricing>()
                .HasData(
                    new Pricing
                    {
                        PricingId = 1,
                        Name = "Orange Cash-Out",
                        Url = @"https://www.orange.cm/fr/tarification-orange-money.html",
                        Lines = JsonConvert.SerializeObject(linesCashOutOrange)
                    },
                    new Pricing
                    {
                        PricingId = 2,
                        Name = "MTN Cash-Out",
                        Url = @"https://mtn.cm/fr/momo/fees/",
                        Lines = JsonConvert.SerializeObject(linesCashOutMTN)
                    },
                    new Pricing
                    {
                        PricingId = 3,
                        Name = "Orange Cash-In",
                        Url = @"https://www.orange.cm/fr/tarification-orange-money.html",
                        Lines = JsonConvert.SerializeObject(linesCashInOrange)
                    },
                    new Pricing
                    {
                        PricingId = 4,
                        Name = "MTN Cash-In",
                        Url = @"https://mtn.cm/fr/momo/fees/",
                        Lines = JsonConvert.SerializeObject(linesCashInMTN)
                    });

            //----------------- One-ToMany-One [Provider - Catalog - Service] -----------

            modelBuilder.Entity<Catalog>()
               .HasKey(ps => new { ps.ProviderId, ps.ServiceId });

            modelBuilder.Entity<Catalog>()
                .HasOne(ps => ps.Provider)
                .WithMany(p => p.Catalogs)
                .HasForeignKey(ps => ps.ProviderId)
                .IsRequired();

            modelBuilder.Entity<Catalog>()
               .HasOne(ps => ps.Service)
               .WithMany(s => s.Catalogs)
               .HasForeignKey(ps => ps.ServiceId)
               .IsRequired();

            modelBuilder.Entity<Catalog>()
                .HasData(
                    new Catalog //Orange retrait
                    {
                        ProviderId = 1,
                        ServiceId = 11,
                        PricingId = 1
                    },
                    new Catalog //Orange dépôt
                    {
                        ProviderId = 1,
                        ServiceId = 12,
                        PricingId = 3
                    },
                    new Catalog //MTN retrait
                    {
                        ProviderId = 2,
                        ServiceId = 11,
                        PricingId = 2
                    },
                    new Catalog //MTN dépôt
                    {
                        ProviderId = 2,
                        ServiceId = 12,
                        PricingId = 4
                    },
                    new Catalog //Orange reçus
                    {
                        ProviderId = 1,
                        ServiceId = 13,
                        PricingId = null
                    });

            modelBuilder.Entity<Catalog>()
                .Navigation(s => s.Pricing)
                .AutoInclude();

            modelBuilder.Entity<Catalog>()
                .Navigation(s => s.Provider)
                .AutoInclude();
        }
    }
}
