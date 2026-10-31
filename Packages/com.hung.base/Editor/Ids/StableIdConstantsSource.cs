using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;

namespace Hung.Base.Editor
{
    /// <summary>Editor source that reflects public static fields and properties of type <typeparamref name="TId"/> on a constants class.</summary>
    /// <typeparam name="TId">The identifier struct type.</typeparam>
    public sealed class StableIdConstantsSource<TId> : IStableIdEditorSource<TId>, IEquatable<StableIdConstantsSource<TId>>
        where TId : struct, IStableId, IEquatable<TId>
    {
        private readonly Type constantsType;

        /// <summary>Initializes a new instance of the <see cref="StableIdConstantsSource{TId}"/> class.</summary>
        /// <param name="constantsType">The constants class type to inspect.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="constantsType"/> is null.</exception>
        public StableIdConstantsSource(Type constantsType)
        {
            this.constantsType = constantsType ?? throw new ArgumentNullException(nameof(constantsType));
        }

        /// <summary>Gets the reflected constants class type.</summary>
        public Type ConstantsType => constantsType;

        /// <inheritdoc />
        public IEnumerable<StableIdEditorOption<TId>> GetOptions()
        {
            foreach (FieldInfo field in constantsType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(TId)) continue;
                object val;
                try { val = field.GetValue(null); }
                catch { continue; }

                if (val is TId id && id.IsValid)
                    yield return new StableIdEditorOption<TId>(id, field.Name, StableIdGrammar.GetGroup(id.Value));
            }

            foreach (PropertyInfo property in constantsType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType != typeof(TId) || property.GetMethod == null) continue;
                object val;
                try { val = property.GetValue(null); }
                catch { continue; }

                if (val is TId id && id.IsValid)
                    yield return new StableIdEditorOption<TId>(id, property.Name, StableIdGrammar.GetGroup(id.Value));
            }
        }

        /// <inheritdoc />
        public bool Equals(StableIdConstantsSource<TId> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return constantsType == other.constantsType;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is StableIdConstantsSource<TId> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return constantsType != null ? constantsType.GetHashCode() : 0;
        }
    }
}
