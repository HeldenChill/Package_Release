using Hung.Base.Persistence;
using Newtonsoft.Json.Linq;

namespace Hung.Data.Persistence
{
    public sealed class RewardSaveMigrations : ISaveMigration
    {
        public int FromVersion => 0;

        public int ToVersion => 1;

        public JObject Migrate(JObject source)
        {
            return source == null ? new JObject() : (JObject)source.DeepClone();
        }
    }
}
