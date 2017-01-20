﻿using System.Collections.Generic;
using System.Linq;
using DbLocalizationProvider.Import;
using DbLocalizationProvider.Queries;
using Xunit;

namespace DbLocalizationProvider.Tests.ImporterTests {
    public class ChangesDetectionTests {
        [Fact]
        public void ImportSome_EmptyDatabase_OnlyInserts() {
            var language = new LocalizationLanguage() { Name = "en" };
            var incoming = new List<LocalizationResource>
            {
                new LocalizationResource("key1")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 1" }
                    }
                },
                new LocalizationResource("key2")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 2" }
                    }
                }
            };

            ConfigurationContext.Setup(cfg => {
                cfg.TypeFactory.ForQuery<GetAllResources.Query>().SetHandler<SomeResourcesQueryHandler>();
            });

            var sut = new ResourceImporter();
            var result = sut.DetectChanges(incoming);

            Assert.Equal(incoming.Count, result.Count(c => c.ChangeType == ChangeType.Insert));
        }

        [Fact]
        public void ImportSome_TheSameInDatabase_OnlyInserts() {
            var language = new LocalizationLanguage() { Name = "en" };
            var incoming = new List<LocalizationResource>
            {
                new LocalizationResource("key3")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 1" }
                    }
                },
                new LocalizationResource("key4")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 2" }
                    }
                }
            };

            ConfigurationContext.Setup(cfg => {
                cfg.TypeFactory.ForQuery<GetAllResources.Query>().SetHandler<SomeResourcesQueryHandler>();
            });

            var sut = new ResourceImporter();
            var result = sut.DetectChanges(incoming);

            Assert.Equal(1, result.Count(c => c.ChangeType == ChangeType.Insert));
            Assert.Equal(0, result.Count(c => c.ChangeType == ChangeType.Update));
        }

        [Fact]
        public void ImportSome_TheSameWithDifferentTranslationInDatabase_InsertsAndUpdates() {
            var language = new LocalizationLanguage() { Name = "en" };
            var incoming = new List<LocalizationResource>
            {
                new LocalizationResource("key5")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 1" }
                    }
                },
                new LocalizationResource("key4")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = language, Value = "Resource 2 - Changed!" }
                    }
                }
            };

            ConfigurationContext.Setup(cfg => {
                cfg.TypeFactory.ForQuery<GetAllResources.Query>().SetHandler<SomeResourcesQueryHandler>();
            });

            var sut = new ResourceImporter();

            var result = sut.DetectChanges(incoming);


            Assert.Equal(1, result.Count(c => c.ChangeType == ChangeType.Insert));
            Assert.Equal(1, result.Count(c => c.ChangeType == ChangeType.Update));
        }
    }


    //public class NoResourcesQueryHandler : IQueryHandler<GetAllResources.Query, IEnumerable<LocalizationResource>> {
    //    public IEnumerable<LocalizationResource> Execute(GetAllResources.Query query) {
    //        return new List<LocalizationResource>();
    //    }
    //}

    public class SomeResourcesQueryHandler : IQueryHandler<GetAllResources.Query, IEnumerable<LocalizationResource>> {
        public IEnumerable<LocalizationResource> Execute(GetAllResources.Query query) {
            return new List<LocalizationResource>
            {
                new LocalizationResource("key4")
                {
                    Translations = new List<LocalizationResourceTranslation>
                    {
                        new LocalizationResourceTranslation { Language = new LocalizationLanguage() { Name= "en" }, Value = "Resource 2" }
                    }
                }
            };
        }
    }
}
