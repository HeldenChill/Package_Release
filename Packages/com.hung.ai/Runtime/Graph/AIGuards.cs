namespace Hung.AI
{
    internal static class AIGuards
    {
        /// <summary>
        /// A branch has a lower-priority guard when it is a ConditionNode flagged LowerPriority, or a SequenceNode
        /// whose leading ConditionNodes include one flagged LowerPriority. The guard passes when every leading condition is true.
        /// </summary>
        public static bool LowerPriorityGuardPasses(AINode branch, AIContext context)
        {
            if (branch is ConditionNode condition)
                return (condition.AbortType & AbortType.LowerPriority) != 0 && condition.Evaluate(context);
            if (branch is SequenceNode sequence)
            {
                var kids = sequence.Children;
                bool flagged = false;
                int leading = 0;
                while (leading < kids.Count && kids[leading] is ConditionNode c)
                {
                    if ((c.AbortType & AbortType.LowerPriority) != 0) flagged = true;
                    leading++;
                }
                if (!flagged) return false;
                for (int i = 0; i < leading; i++)
                    if (!((ConditionNode)kids[i]).Evaluate(context)) return false;
                return true;
            }
            return false;
        }
    }
}
