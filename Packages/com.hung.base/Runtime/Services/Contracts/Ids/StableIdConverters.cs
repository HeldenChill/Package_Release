using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Hung.Base
{
    /// <summary>Generic JSON converter base for types implementing <see cref="IStableId"/>.</summary>
    /// <typeparam name="TId">The typed identifier struct type.</typeparam>
    public abstract class StableIdJsonConverter<TId> : JsonConverter<TId>
        where TId : struct, IStableId
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, TId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        /// <inheritdoc />
        public override TId ReadJson(
            JsonReader reader,
            Type objectType,
            TId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                throw new JsonSerializationException($"Invalid {typeof(TId).Name} token '{reader.TokenType}'.");

            string raw = (string)reader.Value;
            if (TryParse(raw, out TId id))
                return id;

            throw new JsonSerializationException($"Invalid {typeof(TId).Name} value '{raw}'.");
        }

        /// <summary>Attempts to parse the raw string into an instance of <typeparamref name="TId"/>.</summary>
        /// <param name="raw">The raw string value.</param>
        /// <param name="id">The parsed identifier, or default if parsing failed.</param>
        /// <returns><c>true</c> if successfully parsed; otherwise, <c>false</c>.</returns>
        protected abstract bool TryParse(string raw, out TId id);
    }

    /// <summary>Generic type converter base for types implementing <see cref="IStableId"/>.</summary>
    /// <typeparam name="TId">The typed identifier struct type.</typeparam>
    public abstract class StableIdTypeConverter<TId> : TypeConverter
        where TId : struct, IStableId
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string raw)
                return Parse(raw);

            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        /// <inheritdoc />
        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is TId id)
                return id.Value;

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Parses the raw string into an instance of <typeparamref name="TId"/>.</summary>
        /// <param name="raw">The raw string value.</param>
        /// <returns>The parsed identifier.</returns>
        protected abstract TId Parse(string raw);
    }
}
