using Form = System.Windows.Forms.Form;

namespace CodeLab.Forms.Bases
{
    /// <summary>
    /// A form with a dropdown (ComboBox) to select one object.
    /// Shows the keys to the user and returns the matching value in Tag.
    /// </summary>
    public partial class BaseDropdown : Form
    {

        // Form Properties
        private List<string> Keys;
        private List<object> Values;
        private int DefaultIndex;

        /// <summary>
        /// Constructs a dropdown form.
        /// </summary>
        /// <param name="keys">Keys to display in the dropdown.</param>
        /// <param name="values">Values associated to the keys (same order).</param>
        /// <param name="title">A title to display.</param>
        /// <param name="message">A message to display above the dropdown.</param>
        /// <param name="defaultIndex">An optional index to initialize at.</param>
        public BaseDropdown(List<string> keys, List<object> values, string title, string message, int defaultIndex = -1)
        {
            InitializeComponent();
            CodeLab.Utilities.File_Utilities.SetFormIcon(this);

            this.Keys = keys;
            this.Values = values;
            this.DefaultIndex = defaultIndex;
            this.Text = title;
            this.labelMessage.Text = message;
            this.DialogResult = DialogResult.Cancel;
            this.Tag = null;

            PopulateComboBox();
        }

        /// <summary>
        /// Loads the keys to the dropdown and selects the default index (or the first item).
        /// </summary>
        private void PopulateComboBox()
        {
            this.comboBox.Items.Clear();

            foreach (var key in this.Keys)
            {
                this.comboBox.Items.Add(key);
            }

            if (this.DefaultIndex >= 0 && this.DefaultIndex < this.comboBox.Items.Count)
            {
                this.comboBox.SelectedIndex = this.DefaultIndex;
            }
            else   
            {
                try 
                {
                   this.comboBox.SelectedIndex = 0;
                }

                catch 
                {
                    this.comboBox.SelectedIndex = -1;
                }
                
            }
        }

        /// <summary>
        /// Runs when Cancel is clicked. Closes the form (DialogResult stays Cancel).
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Runs when OK is clicked. Stores the value of the selected key in Tag and sets DialogResult to OK.
        /// Setting DialogResult already closes the form.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (this.comboBox.SelectedIndex >= 0)
            {
                var selectedValue = this.Values[this.comboBox.SelectedIndex];
                this.Tag = selectedValue;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.Tag = null;
                this.DialogResult = DialogResult.Cancel;
            }

        }

    }
}
