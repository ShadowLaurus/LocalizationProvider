using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DbLocalizationProvider.DataAnnotations {
    public class CompositeModelMetadataProvider<TProvider> : ModelMetadataProvider where TProvider : ModelMetadataProvider, new() {
        private readonly ModelMetadataProvider _innerProvider;
        private readonly TProvider _wrappedProvider;

        public CompositeModelMetadataProvider(ModelMetadataProvider innerProvider) {
            _innerProvider = innerProvider;
            _wrappedProvider = new TProvider();
        }

        public override IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType) {
            //fix problems with custom ModelMetadataProvider in _innerProvider
            IEnumerable<ModelMetadata> wrapped_metadatas = _wrappedProvider.GetMetadataForProperties(container, containerType);

            if (_innerProvider == null)
                return wrapped_metadatas;

            IEnumerable<ModelMetadata> inner_metadatas = _innerProvider.GetMetadataForProperties(container, containerType);

            // additionalMetadata.Properties.Count() == metadata.Properties.Count() should be always true
            if (wrapped_metadatas != null && inner_metadatas != null && inner_metadatas.Count() > 0 && inner_metadatas.Count() == wrapped_metadatas.Count()) {
                foreach (ModelMetadata inner_metadata in inner_metadatas) {
                    ModelMetadata wrapped_metadata = wrapped_metadatas.Where(x => x.PropertyName == inner_metadata.PropertyName).SingleOrDefault();

                    MergeRecursiveAdditionalValues(wrapped_metadata, inner_metadata);
                }
            }

            return wrapped_metadatas;
        }

        public override ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName) {
            ModelMetadata metadata = _wrappedProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName);

            if (_innerProvider == null)
                return metadata;

            ModelMetadata additionalMetadata = _innerProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName);
            MergeAdditionalValues(metadata.AdditionalValues, additionalMetadata.AdditionalValues);

            return metadata;
        }

        private void MergeAdditionalValues(IDictionary<string, object> target, Dictionary<string, object> source) {
            foreach (var key in source.Keys) {
                if (!target.ContainsKey(key)) {
                    target.Add(key, source[key]);
                }
            }
        }

        private void MergeRecursiveAdditionalValues(ModelMetadata wrapped, ModelMetadata inner, int depth = 0) {
            MergeAdditionalValues(wrapped.AdditionalValues, inner.AdditionalValues);

            // inner.Properties.Count() == wrapped.Properties.Count() should be always true
            if (inner.Properties != null && wrapped.Properties != null && inner.Properties.Count() > 0 && inner.Properties.Count() == wrapped.Properties.Count()) {
                foreach (ModelMetadata innerProperty in inner.Properties) {
                    // Add security to limit StackOverFlow
                    if (innerProperty.ContainerType != null) {
                        if (innerProperty.ContainerType.IsPrimitive || innerProperty.ContainerType.IsEnum || innerProperty.ContainerType == typeof(DateTime))
                            continue;
                    }

                    ModelMetadata wrappedProperty = wrapped.Properties.Where(x => x.PropertyName == innerProperty.PropertyName).SingleOrDefault();

                    if (innerProperty.AdditionalValues != null && innerProperty.AdditionalValues.Count > 0) {
                        MergeAdditionalValues(wrappedProperty.AdditionalValues, innerProperty.AdditionalValues);
                    }
                    //limit recursive depth to 5 due to complexe object like DateTime
                    if (innerProperty.Properties != null && innerProperty.Properties.Count() > 0 && depth < 5) {
                        MergeRecursiveAdditionalValues(wrappedProperty, innerProperty, depth++);
                    }
                }
            }
        }

        public override ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType) {
            //fix problems with custom ModelMetadataProvider in _innerProvider
            ModelMetadata wrapped_metadata = _wrappedProvider.GetMetadataForType(modelAccessor, modelType);

            if (_innerProvider == null)
                return wrapped_metadata;

            ModelMetadata inner_metadata = _innerProvider.GetMetadataForType(modelAccessor, modelType);
            MergeRecursiveAdditionalValues(wrapped_metadata, inner_metadata);

            return wrapped_metadata;
        }
    }
}
