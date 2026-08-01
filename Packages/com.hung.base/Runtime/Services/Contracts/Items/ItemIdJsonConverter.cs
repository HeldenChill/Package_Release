using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Hung.Base
{
    public sealed class ItemIdJsonConverter : JsonConverter<ItemId>
    {
        public override void WriteJson(JsonWriter writer, ItemId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override ItemId ReadJson(
            JsonReader reader,
            Type objectType,
            ItemId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                throw new JsonSerializationException($"Invalid ItemId token '{reader.TokenType}'.");

            string raw = (string)reader.Value;
            if (ItemId.TryParse(raw, out ItemId id))
                return id;

            throw new JsonSerializationException($"Invalid ItemId value '{raw}'.");
        }
    }

    public sealed class ItemIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string raw)
                return ItemId.Parse(raw);

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is ItemId id)
                return id.Value;

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
