# Opret Mapper/Converter
    public static class DtoConverter
    {
        public static T CopyPropertiesTo<T>(this Object sourceObject, T destinationObject)
        {
            foreach (PropertyInfo destinationProperty in destinationObject.GetType().GetProperties().Where(p => p.CanWrite))
            {
                if (sourceObject.GetType().GetProperties().Any(sourceProperty => sourceProperty.Name == destinationProperty.Name && sourceProperty.PropertyType == destinationProperty.PropertyType))
                {
                    PropertyInfo sourceProperty = sourceObject.GetType().GetProperty(destinationProperty.Name);
                    destinationProperty.SetValue(destinationObject, sourceProperty.GetValue(sourceObject));
                }
            }
            return destinationObject;
        }
    }

# Opret extension metoder
    public static class ConverterExtensionMethods
    {
        public static MovieDTO ToDto(this Movie movieToConvert)
        {
            var movieDto = new MovieDTO();
            movieToConvert.CopyPropertiesTo(movieDto);
            return movieDto;
        }
    }
