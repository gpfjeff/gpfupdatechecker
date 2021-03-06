/* Tester.cs
 * 
 * PROGRAMMER:    Jeffrey T. Darlington
 * DATE:          May 18, 2010
 * PROJECT:       GPFUpdateChecker
 * .NET VERSION:  2.0
 * REQUIRES:      UpdateChecker
 * REQUIRED BY:   (None)
 * 
 * This class represents a very simple Windows application that acts as a shell for testing
 * the GPFUpdateChecker library.  It opens a small window then launches the update checker
 * process.  Feel free to tinker with the various settings below to test the update checker
 * and make sure it works for your situation.
 * 
 * UPDATES FOR VERSION 1.1:  A few tweaks to match the version 1.1 changes to the library.
 * 
 * UPDATES FOR VERSION 1.2:  A few tweaks to match the version 1.2 changes to the library.
 * 
 * This program is Copyright 2013, Jeffrey T. Darlington.
 * E-mail:  jeff@gpf-comics.com
 * Web:     https://github.com/gpfjeff/gpfupdatechecker
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
using com.gpfcomics.UpdateChecker;

namespace com.gpfcomics.updatetest
{
    public partial class Tester : Form, IUpdateCheckListener
    {
        /// <summary>
        /// The <see cref="Uri"/> where the update feed XML is located.  The default here is the
        /// Cryptnos update feed; please change this to your own feed to test your update process.
        /// Note that the update checker constructor can take either a Uri or a URL string.
        /// </summary>
        private Uri feedUri = new Uri("http://www.cryptnos.com/files/cryptnos_updates_feed.xml");

        /// <summary>
        /// The application name string.  This should match the "name" tag under "app" in the
        /// update feed XML.  This is how the update checker identifies your app if the feed
        /// contains multiple apps.
        /// </summary>
        private string appName = "Cryptnos for Windows";

        /// <summary>
        /// The current <see cref="Version"/> of your application.  Ideally this should come
        /// from Assembly.GetExecutingAssembly().GetName().Version, but for testing purposes
        /// we'll hard-code this to a test value.  For the update check to declare that the
        /// "current" version is out of date, this must be less than the version in the feed.
        /// </summary>
        private Version currentVersion = new Version("1.0.0.0");

        /// <summary>
        /// The <see cref="DateTime"/> corresponding to the last update check.  By default,
        /// the update checker checks for new updates after one week, or seven days.  As a
        /// test, we'll hard-code this to eight days in the past.  If you set this to a more
        /// recent date, the update checker will silently exist.
        /// </summary>
        private DateTime lastUpdate = DateTime.Now.AddDays(-8);

        /// <summary>
        /// The update check interval, in days.  The <see cref="UpdateChecker"/> requires us
        /// to specify an interval of at least one day, but we can set this to any number of
        /// whole days greater than that.  Seven days (or one week) is a good interval for most
        /// applications.  Security applications may want to check more frequently, while apps
        /// that change infrequently may want to check maybe once per month (30 days).
        /// </summary>
        private int updateInterval = 7;

        /// <summary>
        /// A reference to our <see cref="UpdateChecker"/> object.
        /// </summary>
        private UpdateChecker.UpdateChecker updateChecker = null;

        /// <summary>
        /// This flag turns on (true) or off (false) the detailed debug messages.  In a
        /// production application, you should always send false in the <see cref="UpdateChecker"/>
        /// constructor.  For testing, however, setting it to true will provide detailed
        /// exception information.
        /// </summary>
        private bool debug = true;

        /// <summary>
        /// This flag sets the behavior if a new update has been found.  If true, we'll download an
        /// installer and attempt to run it automatically.  If false, we'll notify the user about the
        /// update but they'll have to download it themselves.  This is just an placeholder for this
        /// test application; a real application will have to decide for itself which case it should
        /// use.  One example where BOTH behavior might be needed is a Windows application that can
        /// potentially be run under Mono on another platform (such as Linux or Mac OS).  You may
        /// want to download and execute an installer on Windows, but simply notify the user if they
        /// are running under Mono.
        /// </summary>
        private bool downloadUpdate = false;

        /// <summary>
        /// If downloadUpdate is set to false, the update checker will attempt to open the user's
        /// default Web browser to this URL.  Presumably, this will be where the user can download
        /// the update manually.  Of course, this is only a sample; your application will need to
        /// set whatever URL it needs.  We recommend, however, that you point to a Web page where
        /// the user can make their own choice about downloading the app, rather than a binary,
        /// archive, or installer file.
        /// </summary>
        private string notifyOnlyUrl = "http://www.cryptnos.com/";

        public Tester()
        {
            // Asbestos underpants:
            try
            {
                // Set up our token window:
                InitializeComponent();
                // Initialize the update checker.  Note that we feed in all the parameters
                // set above, and that we ourselves will be listening for updates from the
                // update checker events.
                updateChecker = new UpdateChecker.UpdateChecker(feedUri, appName,
                    currentVersion, this, lastUpdate, updateInterval, debug);
                // Initiate the update check.  At this point, the update checker will launch
                // a new thread and run in the background.  In a real application, this is
                // where we'll hand control over to the user.  They'll be able to use the
                // application while the update check runs in the background.
                updateChecker.CheckForNewVersion();
            }
            // If something blows up, notify the user.  In a real application, you'll
            // probably want something more useful and user friendly.
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// The IUpdateCheckListener members provide a call-back interface to let the update
        /// checker talk with the parent application.  There are three call-back methods, each
        /// of which are required.  See <see cref="IUpdateCheckListener"/> for additional comments.
        /// </summary>
        #region IUpdateCheckListener Members

        public void OnFoundNewerVersion()
        {
            // This part is usually pretty simple.  If the update checker found a new version, it
            // notifies the parent app through OnFoundNewerVersion().  This gives the parent a
            // chance to do something useful like saving data before starting the update process.
            // Note that the dialog box that notifies the user that an update has been found does
            // not appear until UpdateChecker.GetNewerVersion() is called, so the user doesn't know
            // about the new version yet.
            //
            // Do whatever needs to be done before the user is notified, then call this method as
            // the final step:
            if (downloadUpdate) updateChecker.GetNewerVersion();

            // Conversely, if we don't to actually download the update automatically, we can
            // notify the user and give them the option to download the update manually.  The
            // UpdateChecker.NotifyUserOfNewVersionOnly() method displays a prompt to download
            // the update similar to GetNewerVersion(), but instead of downloading an installer,
            // it opens a URL so the user can download it themselves.
            else updateChecker.NotifyUserOfNewVersionOnly(notifyOnlyUrl);
        }

        public void OnNoUpdateFound()
        {
            // The simplest implementation for this method is to just do nothing; then in no
            // updates were found, the main app will silently ignore this notification and let the
            // user go about their business.  However, if the application wishes to implement a
            // button to let the user initiate a check, the user will probably want to know if no
            // update was found.  Then the main app can implement this method (as we have done here)
            // to display a notification that no update was found.
            MessageBox.Show("No updates were found. You have the latest version of this application.",
                "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void OnUpdateCheckError()
        {
            // The UpdateChecker handles most of its own error messages, but in
            // some instances the calling application may want to perform certain tasks when
            // an error in the update check occurs.  The simplest implementation here would be
            // to do nothing and let the UpdateChecker display its own messages.  However, if
            // the caller wishes to perform some operation when an error occurs, it should
            // put that code here.
            MessageBox.Show("This message was generated by the main application because an error " +
                "occurred during the update process.", "Update Check Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        public void OnRecordLastUpdateCheck(DateTime lastCheck)
        {
            // This call-back gives the parent app a chance to record the new last update check
            // date by whatever means it wants to (save to a file, to the registry, etc.).  For
            // our token test app, this isn't very important.  In a real app, however, you will
            // want to store this for future reference, then feed it back in the update checker
            // constructor the next time the application starts.
            lastUpdate = lastCheck;
            //MessageBox.Show("Last update check time has been updated to " +
            //    lastUpdate.ToString(), "Information", MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);
        }

        public void OnRequestGracefulClose()
        {
            // This method gets called when the update has been successfully download, validated,
            // and the installer is ready to run.  At this point, we want to close the parent
            // app so it won't be running when the installer runs.  At this point, the main
            // app should do whatever steps have to be done before closing, then close.  For
            // our token sample app, we'll just close the form.
            Close();
        }

        public void OnDownloadCanceled()
        {
            // In most instances, the application won't care to do anything when the user cancels
            // the download prompt.  However, if they do want to perform some task, this is the
            // call-back to use.
            MessageBox.Show("This dialog was generated by the main application because the user chose " +
                "to cancel the update download.", "Update Check Download Cancelled", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion
    }
}