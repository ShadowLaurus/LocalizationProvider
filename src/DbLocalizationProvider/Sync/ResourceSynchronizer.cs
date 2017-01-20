﻿using System;
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

namespace DbLocalizationProvider.Sync
{
    public class ResourceSynchronizer
    {
        protected virtual string DetermineDefaultCulture()
        {
            return ConfigurationContext.Current.DefaultResourceCulture != null
                       ? ConfigurationContext.Current.DefaultResourceCulture.Name
                       : "en";
        }

        public void DiscoverAndRegister()
        {
            if(!ConfigurationContext.Current.DiscoverAndRegisterResources)
                return;

            var discoveredTypes = TypeDiscoveryHelper.GetTypes(t => t.GetCustomAttribute<LocalizedResourceAttribute>() != null,
                                                               t => t.GetCustomAttribute<LocalizedModelAttribute>() != null);

            // initialize db structures first (issue #53)
            using (var ctx = new LanguageEntities())
            {
                var tmp = ctx.LocalizationResources.FirstOrDefault();
            }

            ResetSyncStatus();

            Parallel.Invoke(
                            () => RegisterDiscoveredResources(discoveredTypes[0]),
                            () => RegisterDiscoveredResources(discoveredTypes[1]));

            if(ConfigurationContext.Current.PopulateCacheOnStartup)
                PopulateCache();
        }

        public void RegisterManually(IEnumerable<ManualResource> resources)
        {
            using (var db = new LanguageEntities())
            {
                foreach (var resource in resources)
                {
                    RegisterIfNotExist(db, resource.Key, resource.Translation, author: "manual");
                }

                db.SaveChanges();
            }
        }

        private void PopulateCache()
        {
            var c = new ClearCache.Command();
            c.Execute();

            var allResources = new GetAllResources.Query().Execute();

            foreach (var resource in allResources)
            {
                var key = CacheKeyHelper.BuildKey(resource.ResourceKey);
                ConfigurationContext.Current.CacheManager.Insert(key, resource);
            }
        }

        private void ResetSyncStatus() {
            using (var db = new LanguageEntities()) {
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

        private void RegisterDiscoveredResources(IEnumerable<Type> types)
        {
            var helper = new TypeDiscoveryHelper();
            var properties = types.SelectMany(type => helper.ScanResources(type)).DistinctBy(r => r.Key);

            using (var db = new LanguageEntities())
            {
                foreach (var property in properties)
                    RegisterIfNotExist(db, property.Key, property.Translation);

                db.SaveChanges();
            }
        }

        private void RegisterIfNotExist(LanguageEntities db, string resourceKey, string resourceValue, string author = "type-scanner")
        {
            var existingResource = db.LocalizationResources.Include(r => r.Translations).FirstOrDefault(r => r.ResourceKey == resourceKey);
            var defaultTranslationCulture = DetermineDefaultCulture();

            if(existingResource != null)
            {
                existingResource.FromCode = true;

                // if resource is not modified - we can sync default value from code
                if(existingResource.IsModified.HasValue && !existingResource.IsModified.Value)
                {
                    existingResource.ModificationDate = DateTime.UtcNow;
                    var defaultTranslation = existingResource.Translations.FirstOrDefault(t => t.Language == defaultTranslationCulture);
                    if(defaultTranslation != null)
                    {
                        defaultTranslation.Value = resourceValue;
                    }
                }

                var fromCodeTranslation = existingResource.Translations.FirstOrDefault(t => t.Language == ConfigurationContext.CultureForTranslationsFromCode);
                if(fromCodeTranslation != null)
                {
                    fromCodeTranslation.Value = resourceValue;
                }
                else
                {
                    fromCodeTranslation = new LocalizationResourceTranslation
                    {
                        Language = ConfigurationContext.CultureForTranslationsFromCode,
                        Value = resourceValue
                    };

                    existingResource.Translations.Add(fromCodeTranslation);
                }
            }
            else
            {
                // create new resource
                var resource = new LocalizationResource(resourceKey)
                {
                    ModificationDate = DateTime.UtcNow,
                    Author = author,
                    FromCode = true,
                    IsModified = false
                };

                resource.Translations.Add(new LocalizationResourceTranslation
                                          {
                                              Language = defaultTranslationCulture,
                                              Value = resourceValue
                                          });

                resource.Translations.Add(new LocalizationResourceTranslation
                                          {
                                              Language = ConfigurationContext.CultureForTranslationsFromCode,
                                              Value = resourceValue
                                          });
                db.LocalizationResources.Add(resource);
            }
        }
    }
}
