using System.Data.Entity;

namespace DbLocalizationProvider {
    public class LanguageEntities : DbContext {
        static LanguageEntities() {
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<LanguageEntities, Configuration>());
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<LanguageEntities, ConfigurationAutoUpdate>());
        }

        public LanguageEntities() : this(ConfigurationContext.Current.ConnectionName) { }

        public LanguageEntities(string connectionString) : base(connectionString) {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;

            Database.Initialize(false);
        }

        public DbSet<LocalizationResource> LocalizationResources { get; set; }
        public DbSet<LocalizationResourceTranslation> LocalizationResourceTranslations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            if (!string.IsNullOrWhiteSpace(ConfigurationContext.Current.DatabaseSchema)) {
                modelBuilder.Entity<LocalizationResource>().ToTable(nameof(LocalizationResource), ConfigurationContext.Current.DatabaseSchema);
                modelBuilder.Entity<LocalizationResourceTranslation>().ToTable(nameof(LocalizationResourceTranslation), ConfigurationContext.Current.DatabaseSchema);
            }
        }
    }

    internal sealed class ConfigurationAutoUpdate : System.Data.Entity.Migrations.DbMigrationsConfiguration<LanguageEntities> {
        public ConfigurationAutoUpdate() {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
            ContextKey = "DbLocalizationProvider.LanguageEntities";
        }
    }
}