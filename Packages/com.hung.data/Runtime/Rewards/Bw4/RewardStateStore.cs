using System;
using Hung.Base.Persistence;
using Newtonsoft.Json;
namespace Hung.Data.Rewards
{
    // No cached mutable state; selected feature owns its model and operation serialization.
    public sealed class RewardStateStore<T> where T : new()
    {
        private readonly IPersistenceService persistence;
        private readonly SaveDefinition<T> definition;
        public RewardStateStore(IPersistenceService persistence, SaveDefinition<T> definition)
        { this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence)); this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public bool Load(out T value, out string diagnostic)
        {
            var result = persistence.Load(definition); diagnostic = result.DiagnosticCode;
            value = result.Success ? Copy(result.Value) : default; return result.Success;
        }
        public bool Save(T value, out string diagnostic)
        { var result = persistence.Save(definition, Copy(value)); diagnostic = result.DiagnosticCode; return result.Success; }
        public static T Copy(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }
}
