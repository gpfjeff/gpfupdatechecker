/* ProgressDialog.cs
 * 
 * PROGRAMMER:    Jeffrey T. Darlington
 * DATE:          May 18, 2010
 * PROJECT:       GPFUpdateChecker
 * .NET VERSION:  2.0
 * REQUIRES:      (None)
 * REQUIRED BY:   UpdateChecker
 * 
 * This dialog box displays the progress of UpdateChecker operations.  It can be used in one
 * of two modes:  Download mode or Verification mode.  In each case, the dialog receives
 * updates from a BackgroundWorker which reports its progress on the specified task.  A Cancel
 * button allows the user to cancel the task if they so desire.
 * 
 * This program is Copyright 2010, Jeffrey T. Darlington.
 * E-mail:  jeff@gpf-comics.com
 * Web:     http://www.gpf-comics.com/
 * 
 * This program is free software; you can redistribute it and/or modify it under the terms of
 * the GNU General Public License as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See theGNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with this program;
 * if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace com.gpfcomics.UpdateChecker
{
    /// <summary>
    /// This enumeration specifies what mode the <see cref="ProgressDialog"/> will be called
    /// in.  This allows us to reuse the same dialog resource for multiple purposes.
    /// </summary>
    public enum ProgressDialogMode
    {
        /// <summary>
        /// Put the <see cref="ProgressDialog"/> in download mode
        /// </summary>
        Download,
        /// <summary>
        /// Put the <see cref="ProgressDialog"/> in validation mode
        /// </summary>
        Verify
    }

    /// <summary>
    /// This dialog box displays the progress of <see cref="UpdateChecker"/> operations.  It
    /// can be used in one of two modes:  Download mode or Verification mode.  In each case,
    /// the dialog receives updates from a <see cref="BackgroundWorker"/> which reports its
    /// progress on the specified task.  A Cancel button allows the user to cancel the task
    /// if they so desire.
    /// </summary>
    public partial class ProgressDialog : Form
    {
        /// <summary>
        /// A reference to the <see cref="BackgroundWorker"/> owned by the
        /// <see cref="UpdateChecker"/>.  We will use this to cancel the download process
        /// if the user clicks the Cancel button.
        /// </summary>
        private BackgroundWorker worker = null;

        /// <summary>
        /// The name of the application being downloaded, which will be displayed to the user
        /// when in Download mode.  It is suggested that this not just be the application name,
        /// but perhaps should include the new version number as well.
        /// </summary>
        private string appName = null;

        /// <summary>
        /// The current mode the dialog is operating in
        /// </summary>
        private ProgressDialogMode mode;

        /// <summary>
        /// The ProgressDialog constructor
        /// </summary>
        /// <param name="worker">The <see cref="BackgroundWorker"/> owned by the
        /// <see cref="UpdateChecker"/></param>
        /// <param name="appName">The name of the application being downloaded</param>
        /// <param name="mode">The <see cref="ProgressDialogMode"/> the dialog has been
        /// called in</param>
        public ProgressDialog(BackgroundWorker worker, string appName, ProgressDialogMode mode)
        {
            InitializeComponent();
            this.worker = worker;
            this.appName = appName;
            this.mode = mode;
            // Depending on which mode we're called in, update the label to show what
            // we're doing.  Note that the download mode includes the app name.
            if (mode == ProgressDialogMode.Download)
                lblDownload.Text = "Downloading " + appName + "...";
            else lblDownload.Text = "Validating download...";
        }

        /// <summary>
        /// Update the progress dialog's progress bar with the current completion percentage
        /// </summary>
        /// <param name="percentage"></param>
        public void UpdateProgress(int percentage)
        {
            progressBar1.Value = percentage;
        }

        /// <summary>
        /// What to do when the Cancel button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Let's customize the cancel confirmation message depending on the mode we're
            // in.  Otherwise, it might be a bit confusing.
            string message = "Are you sure you want to cancel this download?";
            string title = "Cancel Download";
            if (mode == ProgressDialogMode.Verify)
            {
                message = "Are you sure you want to cancel validation of this download?";
                title = "Cancel Validation";
            }
            // Double-check with the user that they really want to cancel the process:
            if (MessageBox.Show(message, title, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // If they do, cancel the BackgroundWorker and close up shop:
                try
                {
                    btnCancel.Enabled = false;
                    if (worker != null) worker.CancelAsync();
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
                catch
                {
                    MessageBox.Show("The process could not be cancelled.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

    }
}