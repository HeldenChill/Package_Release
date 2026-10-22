using UnityEngine;

namespace Hung.Base
{
    /// <summary>
    /// Transient context passed through damage calculator and effect pipeline.
    /// Allows skill-specific stat multipliers and logic to apply during execution.
    /// </summary>
    public struct SkillExecutionContext
    {
        /// <summary>
        /// Unique identifier of the skill currently executing.
        /// </summary>
        public string SkillId { get; set; }

        /// <summary>
        /// Reference to the skill instance (optional; for future callbacks).
        /// </summary>
        public object SkillInstance { get; set; }

        /// <summary>
        /// True if context is valid and should be used in calculations.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(SkillId);

        public SkillExecutionContext(string skillId, object skillInstance = null)
        {
            SkillId = skillId;
            SkillInstance = skillInstance;
        }

        public static SkillExecutionContext Empty => new SkillExecutionContext(string.Empty, null);
    }
}
