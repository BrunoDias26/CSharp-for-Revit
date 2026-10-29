namespace CodeLab.Utilities
{
    public static class Form_Utilities
    {
        /// <summary>
        /// Calculates a suitable progress delay (sleep) for each step of a progress bar.
        /// </summary>
        /// <param name="steps">Steps to take as progress.</param>
        /// <param name="duration">Desired overall duration, in milliseconds.</param>
        /// <param name="imposeLimit">Enforces a delay between 1 and 500ms per step.</param>
        /// <returns>The delay per step, in milliseconds.</returns>
        public static int ProgressDelay(int steps, int duration = 3000, bool imposeLimit = true)
        {
            // If step is 1 or less, we last the full duration
            if (steps < 2) { return duration; }

            // Calculate delay time step (integer division: 3000 / 7 = 428)
            int step = duration / steps;

            // Enforce a logical limit
            if (imposeLimit)
            {
                if (step < 1) { step = 1; }
                else if (step > 500) { step = 500; }
            }

            return step;
        }
    }
}
