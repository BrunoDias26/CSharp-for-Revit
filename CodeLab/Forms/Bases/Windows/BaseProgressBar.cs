using Form = System.Windows.Forms.Form;
using FormApplication = System.Windows.Forms.Application;

namespace CodeLab.Forms.Bases
{
    /// <summary>
    /// A non-modal progress bar form with a Cancel button, used while a task runs.
    /// The command calls Increment each step, CancelCheck before each step and Commit at the end.
    /// </summary>
    public partial class BaseProgressBar : Form
    {

        // Form Properties
        private bool Cancelled;
        private int ProgressCount;
        private int ProgressTotal;
        private bool ShowProgress;
        private bool Cancellable;
        private string TaskName;

        /// <summary>
        /// Implements a progress bar form.
        /// </summary>
        /// <param name="taskName">Name of the task to run.</param>
        /// <param name="pbTotal">Total number of steps.</param>
        /// <param name="showProgress">Report progress to the user.</param>
        /// <param name="cancellable">Can the form be cancelled.</param>
        public BaseProgressBar(string taskName = null, int pbTotal = 100, bool showProgress = true, bool cancellable = true)
        {
            InitializeComponent();

            // Set progress values
            this.ProgressCount = 1;
            this.ProgressTotal = pbTotal;
            this.progressBarObject.Minimum = 0;
            this.progressBarObject.Maximum = pbTotal;
            this.progressBarObject.Value = 1;

            // Set label properties
            taskName ??= "Processing task";
            this.TaskName = taskName;
            this.labelProgress.Text = taskName;
            this.ShowProgress = showProgress;

            // Cancellation properties
            this.Cancelled = false;
            this.Cancellable = cancellable;
            this.buttonCancel.Enabled = cancellable;

            // Show the form (non-modal), then let Windows draw it
            Show();
            FormApplication.DoEvents();
        }

        /// <summary>
        /// Increase progress by one step.
        /// </summary>
        public void Increment()
        {
            // If we won't exceed the total, increase count by 1
            if (this.ProgressCount < this.ProgressTotal)
            {
                this.ProgressCount++;
            }

            this.progressBarObject.Value = this.ProgressCount;

            // Go back one and forward again: forces Windows to draw the bar now instead of animating late
            if (this.ProgressCount > 1)
            {
                this.progressBarObject.Value = this.ProgressCount - 1;
                this.progressBarObject.Value = this.ProgressCount;
            }

            // If we haven't cancelled and show progress, update the task description
            if (this.ShowProgress && !this.Cancelled)
            {
                this.labelProgress.Text = $"{this.TaskName} ({this.ProgressCount} of {this.ProgressTotal})";
            }

            // Let Windows redraw the form and process clicks (e.g. Cancel)
            FormApplication.DoEvents();
        }

        /// <summary>
        /// Checks for cancellation, rolling back the transaction if cancelled.
        /// </summary>
        /// <param name="t">Related transaction to the task.</param>
        /// <returns>True if the user cancelled.</returns>
        public bool CancelCheck(Transaction t = null)
        {
            // If we cancelled and have a transaction, roll it back
            if (this.Cancelled && t is Transaction)
            {
                t.RollBack();
            }

            return this.Cancelled;
        }

        /// <summary>
        /// Commits a transaction if we didn't cancel.
        /// </summary>
        /// <param name="t">Related transaction to the task.</param>
        public void Commit(Transaction t)
        {
            if (!this.Cancelled)
            {
                t.Commit();
            }
        }

        /// <summary>
        /// Cancel the progress bar.
        /// </summary>
        private void Cancel()
        {
            // If we haven't cancelled, run a cancellation
            if (!this.Cancelled)
            {
                this.Cancelled = true;
                this.labelProgress.Text = "Cancelling task...";
                this.buttonCancel.Enabled = false;
            }
        }

        /// <summary>
        /// Runs when Cancel is clicked. Only raises the Cancelled flag: the command stops at its next CancelCheck.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Cancel();
        }

    }
}
