namespace Hung.AutoTest
{
    public sealed class NoNaNTransformAssertion : AutoTestAssertionBase
    {
        public override string Id { get { return "NoNaNTransformAssertion"; } }

        public NoNaNTransformAssertion(AutoTestAssertionConfig config) : base(config) { }

        public override AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context)
        {
            if (snapshot == null || snapshot.importantTransforms == null)
                return AutoTestAssertionResult.Passed(Id);

            for (int i = 0; i < snapshot.importantTransforms.Count; i++)
            {
                EntitySnapshot entity = snapshot.importantTransforms[i];
                if (entity != null && entity.hasInvalidTransform)
                    return AutoTestAssertionResult.Failed(Id, "Invalid transform detected: " + entity.path);
            }

            return AutoTestAssertionResult.Passed(Id);
        }
    }
}
