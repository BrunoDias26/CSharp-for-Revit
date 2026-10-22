using Form = System.Windows.Forms.Form;

namespace CSharp_for_Revit.Forms.Bases
{
    /// <summary>
    /// A form with a list to select one object (single select) or several (checkboxes).
    /// Shows the keys to the user and returns the matching value(s) in Tag.
    /// </summary>
    public partial class BaseListView : Form
    {

        // Form Properties
        private bool MultiSelect;
        private List<string> Keys;
        private List<object> Values;

        /// <summary>
        /// Constructs a listview form.
        /// </summary>
        /// <param name="keys">Keys to display in the listview.</param>
        /// <param name="values">Values associated to the keys.</param>
        /// <param name="title">A title to display.</param>
        /// <param name="multiSelect">Select one or more items.</param>
        public BaseListView(List<string> keys, List<object> values, string title, bool multiSelect = true)
        {
            InitializeComponent();
            CSharp_for_Revit.Utilities.File_Utilities.SetFormIcon(this);

            this.Text = title;
            this.Keys = keys;
            this.Values = values;

            // Multiselect behavior
            this.MultiSelect = multiSelect;
            this.listView.MultiSelect = multiSelect;
            this.listView.CheckBoxes = multiSelect;
            this.buttonCheckAll.Enabled = multiSelect;
            this.buttonUncheckAll.Enabled = multiSelect;

            // Cancel by default
            this.DialogResult = DialogResult.Cancel;
            this.Tag = null;

            PopulateListView();
        }

        /// <summary>
        /// Loads the keys to the ListView, one item per key, in the same order as the values.
        /// </summary>
        private void PopulateListView()
        {
            // Clear the listview, add a column
            this.listView.Clear();
            this.listView.Columns.Add("Key", 380);

            // Add each key as an item
            foreach (var key in this.Keys)
            {
                var listViewItem = new ListViewItem(key);
                listViewItem.Checked = false;
                this.listView.Items.Add(listViewItem);
            }
        }

        /// <summary>
        /// Runs when OK is clicked. Multiselect: stores a list of the checked values in Tag.
        /// Single select: stores the selected value in Tag. Nothing chosen: DialogResult stays Cancel.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonOk_Click(object sender, EventArgs e)
        {
            // Multiselect: return every checked item
            if (this.MultiSelect && this.listView.CheckedItems.Count > 0)
            {
                this.Tag = this.listView.CheckedItems
                    .Cast<ListViewItem>()
                    .Select(i => this.Values[i.Index])
                    .ToList();

                this.DialogResult = DialogResult.OK;
            }

            // Single select: return the selected item
            if (!this.MultiSelect && this.listView.SelectedItems.Count > 0)
            {
                int index = this.listView.SelectedItems[0].Index;
                this.Tag = this.Values[index];
                this.DialogResult = DialogResult.OK;
            }

            this.Close();
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
        /// Runs when Check all is clicked. Checks and selects every item (multiselect only).
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonCheckAll_Click(object sender, EventArgs e)
        {
            if (!this.MultiSelect) { return; }

            foreach (ListViewItem item in this.listView.Items)
            {
                item.Checked = true;
                item.Selected = true;
            }
        }

        /// <summary>
        /// Runs when Uncheck all is clicked. Unchecks and unselects every item (multiselect only).
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void buttonUncheckAll_Click(object sender, EventArgs e)
        {
            if (!this.MultiSelect) { return; }

            foreach (ListViewItem item in this.listView.Items)
            {
                item.Checked = false;
                item.Selected = false;
            }
        }

    }
}
