using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DbLocalizationProvider.Cache;
using DbLocalizationProvider.Commands;
using DbLocalizationProvider.Internal;
using DbLocalizationProvider.Queries;
using EntityFramework.Extensions;
using System.Data.Entity.Infrastructure;

namespace DbLocalizationProvider.Sync {
    public class ResourceSynchronizer {
        protected virtual string DetermineDefaultCulture() {
            return ConfigurationContext.Current.DefaultResourceCulture != null
                       ? ConfigurationContext.Current.DefaultResourceCulture.Name
                       : "en";
        }

        public void DiscoverAndRegister() {
            if (!ConfigurationContext.Current.DiscoverAndRegisterResources)
                return;

            var discoveredTypes = TypeDiscoveryHelper.GetTypes(t => t.GetCustomAttribute<LocalizedResourceAttribute>() != null,
                                                               t => t.GetCustomAttribute<LocalizedModelAttribute>() != null);

            // initialize db structures first (issue #53)
            using (var ctx = new LanguageContext()) {
                var tmp = ctx.LocalizationResources.FirstOrDefault();
            }

            ResetSyncStatus();

            //create first language, if not exists
            string defaultTranslationCulture = DetermineDefaultCulture();
            using (var db = new LanguageContext()) {
                if (!db.LocalizationLanguages.Any(x => x.Name == defaultTranslationCulture)) {
                    db.LocalizationLanguages.Add(new LocalizationLanguage() { Name = defaultTranslationCulture });
                    db.SaveChanges();
                }
            }

            Parallel.Invoke(
                            () => RegisterDiscoveredResources(discoveredTypes[0]),
                            () => RegisterDiscoveredResources(discoveredTypes[1]));

            if (ConfigurationContext.Current.PopulateCacheOnStartup)
                PopulateCache();
        }

        public void RegisterManually(IEnumerable<ManualResource> resources) {
            using (var db = new LanguageContext()) {
                foreach (var resource in resources) {
                    RegisterIfNotExist(db, resource.Key, resource.Translation, author: "manual");
                }

                db.SaveChanges();
            }
        }

        private void PopulateCache() {
            var c = new ClearCache.Command();
            c.Execute();

            var allResources = new GetAllResources.Query().Execute();

            foreach (var resource in allResources) {
                var key = CacheKeyHelper.BuildKey(resource.ResourceKey);
                ConfigurationContext.Current.CacheManager.Insert(key, resource);
            }
        }

        private void ResetSyncStatus() {
            using (var db = new LanguageContext()) {
                db.LocalizationResources.Update(t => new LocalizationResource { FromCode = false });
                db.SaveChanges();
            }
            //using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConfigurationContext.Current.ConnectionName].ConnectionString))
            //{
            //    var cmd = new SqlCommand("UPDATE dbo.LocalizationResources SET FromCode = 0", conn);
            //    conn.Open();
            //    cmd.ExecuteNonQuery();
            //    conn.Close();
            //}
        }

        private void RegisterDiscoveredResources(IEnumerable<Type> types) {
            var helper = new TypeDiscoveryHelper();
            var properties = types.SelectMany(type => helper.ScanResources(type)).DistinctBy(r => r.Key);

            using (var db = new LanguageContext()) {
                foreach (var property in properties)
                    RegisterIfNotExist(db, property.Key, property.Translation);

                db.SaveChanges();
            }
        }

        private void RegisterIfNotExist(LanguageContext db, string resourceKey, string resourceValue, string author = "type-scanner") {
            LocalizationResource existingResource = db.LocalizationResources.Include(r => r.Translations)
                                                        .Include(x => x.Translations.Select(y => y.Language))
                                                        .FirstOrDefault(r => r.ResourceKey == resourceKey);
            string defaultTranslationCulture = DetermineDefaultCulture();
            LocalizationLanguage language = null;

            if (db.LocalizationLanguages.Any(x => x.Name == defaultTranslationCulture)) {
                language = db.LocalizationLanguages.Single(x => x.Name == defaultTranslationCulture);
            } else {
                language = new LocalizationLanguage() { Name = defaultTranslationCulture };
                db.LocalizationLanguages.Add(language);

                try {
                    db.SaveChanges();
                } catch (DbUpdateException) {
                    language = db.LocalizationLanguages.Single(x => x.Name == defaultTranslationCulture);
                }
            }

            if (existingResource != null) {
                existingResource.FromCode = true;

                // if resource is not modified - we can sync default value from code
                if (existingResource.IsModified.HasValue && !existingResource.IsModified.Value) {
                    LocalizationResourceTranslation defaultTranslation = existingResource.Translations.FirstOrDefault(t => t.LanguageId == language.Id);
                    existingResource.ModificationDate = DateTime.UtcNow;

                    if (defaultTranslation != null) {
                        defaultTranslation.Value = resourceValue;
                    }
                }

                // search default translation
                LocalizationResourceTranslation fromCodeTranslation = existingResource.Translations.FirstOrDefault(t => t.Language == null);
                if (fromCodeTranslation != null) {
                    fromCodeTranslation.Value = resourceValue;
                } else {
                    fromCodeTranslation = new LocalizationResourceTranslation { Value = resourceValue };
                    existingResource.Translations.Add(fromCodeTranslation);
                }
            } else {
                // create new resource
                var resource = new LocalizationResource(resourceKey) {
                    ModificationDate = DateTime.UtcNow,
                    Author = author,
                    FromCode = true,
                    IsModified = false
                };

                resource.Translations.Add(new LocalizationResourceTranslation() { Language = language, Value = resourceValue });
                resource.Translations.Add(new LocalizationResourceTranslation() { Value = resourceValue });
                db.LocalizationResources.Add(resource);
            }
        }
    }
}
