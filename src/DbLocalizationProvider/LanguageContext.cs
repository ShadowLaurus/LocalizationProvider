using System.Data.Entity;

namespace DbLocalizationProvider {
    public class LanguageContext : DbContext {
        static LanguageContext() {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<LanguageContext, ConfigurationAutoUpdate>());
        }

        public LanguageContext() : base(ConfigurationContext.Current.ConnectionName) { }
        public LanguageContext(string connectionString) : base(connectionString) {
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<LocalizationLanguage> LocalizationLanguages { get; set; }
        public DbSet<LocalizationResource> LocalizationResources { get; set; }
        public DbSet<LocalizationResourceTranslation> LocalizationResourceTranslations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            if (!string.IsNullOrWhiteSpace(ConfigurationContext.Current.DatabaseSchema)) {
                modelBuilder.Entity<LocalizationLanguage>().ToTable(nameof(LocalizationLanguage), ConfigurationContext.Current.DatabaseSchema);
                modelBuilder.Entity<LocalizationResource>().ToTable(nameof(LocalizationResource), ConfigurationContext.Current.DatabaseSchema);
                modelBuilder.Entity<LocalizationResourceTranslation>().ToTable(nameof(LocalizationResourceTranslation), ConfigurationContext.Current.DatabaseSchema);
            }
        }
    }

    internal sealed class ConfigurationAutoUpdate : System.Data.Entity.Migrations.DbMigrationsConfiguration<LanguageContext> {
        public ConfigurationAutoUpdate() {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "DbLocalizationProvider.LanguageEntities";
        }
    }
}