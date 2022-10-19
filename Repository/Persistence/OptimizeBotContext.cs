using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using OptimizeBot.Model;

namespace OptimizeBot.Context
{
    public class OptimizeBotContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Data Source=optimizebot.db");
        }

        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Session> Sessions { get; set; } = default!;
        public DbSet<Pricing> Pricings { get; set; } = default!;
        public DbSet<Service> Services { get; set; } = default!;
        public DbSet<Provider> Providers { get; set; } = default!;
        public DbSet<Catalog> Catalogs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity
                .Property(u => u.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()").Metadata
                .SetAfterSaveBehavior(PropertySaveBehavior.Throw);

                entity
                .HasOne(u => u.Session)
                .WithOne(u => u.User)
                .HasForeignKey<Session>(s => s.UserId)
                .IsRequired();

                entity
                .Navigation(u => u.Session)
                .AutoInclude();
            });
            modelBuilder.Entity<Session>(entity =>
            {
                entity
                .Property(s => s.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()").Metadata
                .SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            });
            modelBuilder.Entity<Provider>(entity =>
            {
                entity
                .Property(p => p.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()").Metadata
                .SetAfterSaveBehavior(PropertySaveBehavior.Throw);

                entity
                .HasOne(p => p.Config)
                .WithOne(c => c.Provider)
                .HasForeignKey<ProviderConfig>(p => p.ProviderId)
                .IsRequired();

                entity
                .Navigation(p => p.Catalogs)
                .AutoInclude();

                entity
                .Navigation(p => p.Config)
                .AutoInclude();

                var utilitiesOrange = new[]
                {
                    new Utility("11", "Camwater Postpaid"),
                    new Utility("13", "Eneo Postpaid"),
                    new Utility("6", "Orange")
                };

                entity
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
            });
            modelBuilder.Entity<Service>(entity =>
            {
                entity
                .Property(s => s.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()").Metadata
                .SetAfterSaveBehavior(PropertySaveBehavior.Throw);

                entity
                .HasOne(s => s.Parent)
                .WithMany(s => s!.Services)
                .HasForeignKey(s => s.ParentId)
                .IsRequired(false);

                entity
                .Navigation(s => s.Catalogs)
                .AutoInclude();

                entity
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
            });
            modelBuilder.Entity<Pricing>(entity =>
            {
                entity
                .Property(pricing => pricing.CreationDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("datetime()")
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

                entity
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

                entity
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
            });
            modelBuilder.Entity<Catalog>(entity =>
            {
                entity
                .HasKey(ps => new { ps.ProviderId, ps.ServiceId });

                entity
                .HasOne(ps => ps.Provider)
                .WithMany(p => p.Catalogs)
                .HasForeignKey(ps => ps.ProviderId)
                .IsRequired();

                entity
                .HasOne(ps => ps.Service)
                .WithMany(s => s.Catalogs)
                .HasForeignKey(ps => ps.ServiceId)
                .IsRequired();

                entity
                .Navigation(s => s.Pricing)
                .AutoInclude();

                entity
                .Navigation(s => s.Provider)
                .AutoInclude();

                entity
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
            });
            modelBuilder
                .Entity<ProviderConfig>()
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
        }
    }
}