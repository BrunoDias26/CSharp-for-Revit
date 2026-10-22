namespace CSharp_for_Revit.Forms.Bases
{
    partial class BaseProgressBar
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            labelProgress = new Label();
            progressBarObject = new ProgressBar();
            buttonCancel = new Button();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(labelProgress, 0, 0);
            tableLayoutPanel1.Controls.Add(progressBarObject, 0, 1);
            tableLayoutPanel1.Controls.Add(buttonCancel, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(434, 234);
            tableLayoutPanel1.TabIndex = 0;
            //
            // labelProgress
            //
            labelProgress.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            labelProgress.AutoSize = true;
            labelProgress.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelProgress.Location = new System.Drawing.Point(16, 35);
            labelProgress.Margin = new Padding(16, 0, 16, 0);
            labelProgress.Name = "labelProgress";
            labelProgress.Size = new Size(402, 17);
            labelProgress.TabIndex = 0;
            labelProgress.Text = "Progress is occurring";
            labelProgress.TextAlign = ContentAlignment.MiddleCenter;
            //
            // progressBarObject
            //
            progressBarObject.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBarObject.Location = new System.Drawing.Point(16, 91);
            progressBarObject.Margin = new Padding(16, 4, 16, 4);
            progressBarObject.Name = "progressBarObject";
            progressBarObject.Size = new Size(402, 52);
            progressBarObject.Style = ProgressBarStyle.Continuous;
            progressBarObject.TabIndex = 1;
            //
            // buttonCancel
            //
            buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonCancel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonCancel.Location = new System.Drawing.Point(16, 151);
            buttonCancel.Margin = new Padding(16, 4, 16, 16);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(402, 67);
            buttonCancel.TabIndex = 2;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            //
            // BaseProgressBar
            //
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 234);
            ControlBox = false;
            Controls.Add(tableLayoutPanel1);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(450, 250);
            Name = "BaseProgressBar";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Progress";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Label labelProgress;
        private ProgressBar progressBarObject;
        private Button buttonCancel;
    }
}
