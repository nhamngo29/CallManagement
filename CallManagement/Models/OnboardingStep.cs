namespace CallManagement.Models
{
    /// <summary>
    /// Represents a step in the onboarding process.
    /// </summary>
    public class OnboardingStep
    {
        /// <summary>
        /// The step number (1-based).
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// The title of the step.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The description of the step.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The target element to highlight.
        /// </summary>
        public string TargetElement { get; set; } = string.Empty;
    }
}
