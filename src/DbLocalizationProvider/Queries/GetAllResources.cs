using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace DbLocalizationProvider.Queries {
    public class GetAllResources {
        public class Query : IQuery<IEnumerable<LocalizationResource>> { }

        public class Handler : IQueryHandler<Query, IEnumerable<LocalizationResource>> {
            public IEnumerable<LocalizationResource> Execute(Query query) {
                using (var db = new LanguageContext()) {
                    return db.LocalizationResources.Include(r => r.Translations)
                        .Include(x => x.Translations.Select(y => y.Language))
                        .ToList();
                }
            }
        }
    }
}
