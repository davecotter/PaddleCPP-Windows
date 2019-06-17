namespace vstest
{
	partial class Form1
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
			this.button_validate = new System.Windows.Forms.Button();
			this.helloWorldLabel = new System.Windows.Forms.Label();
			this.button_purchase = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// button_validate
			// 
			this.button_validate.Location = new System.Drawing.Point(61, 310);
			this.button_validate.Margin = new System.Windows.Forms.Padding(4);
			this.button_validate.Name = "button_validate";
			this.button_validate.Size = new System.Drawing.Size(195, 54);
			this.button_validate.TabIndex = 2;
			this.button_validate.Text = "Validate";
			this.button_validate.UseVisualStyleBackColor = true;
			this.button_validate.Click += new System.EventHandler(this.button1_Click);
			// 
			// helloWorldLabel
			// 
			this.helloWorldLabel.AutoSize = true;
			this.helloWorldLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.helloWorldLabel.Location = new System.Drawing.Point(52, 38);
			this.helloWorldLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.helloWorldLabel.Name = "helloWorldLabel";
			this.helloWorldLabel.Size = new System.Drawing.Size(252, 51);
			this.helloWorldLabel.TabIndex = 3;
			this.helloWorldLabel.Text = "Paddle Test";
			// 
			// button_purchase
			// 
			this.button_purchase.Location = new System.Drawing.Point(300, 310);
			this.button_purchase.Margin = new System.Windows.Forms.Padding(4);
			this.button_purchase.Name = "button_purchase";
			this.button_purchase.Size = new System.Drawing.Size(195, 54);
			this.button_purchase.TabIndex = 4;
			this.button_purchase.Text = "Purchase";
			this.button_purchase.UseVisualStyleBackColor = true;
			this.button_purchase.Click += new System.EventHandler(this.button2_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(613, 440);
			this.Controls.Add(this.button_purchase);
			this.Controls.Add(this.helloWorldLabel);
			this.Controls.Add(this.button_validate);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "Form1";
			this.Text = "Optimum Cats";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button button_validate;
		private System.Windows.Forms.Label helloWorldLabel;
		private System.Windows.Forms.Button button_purchase;
	}
}

