/* UpdateChecker.cs
 * 
 * PROGRAMMER:    Jeffrey T. Darlington
 * DATE:          May 18, 2010
 * PROJECT:       GPFUpdateChecker
 * .NET VERSION:  2.0
 * REQUIRES:      AppMetaData, IUpdateCheckListener, ProgressDialog
 * REQUIRED BY:   (None)
 * 
 * This class is the driver of the GPF Update Checker process.  Simply instantiate this class
 * and pass it the necessary application metadata, and it will go online and search to see if
 * a newer version of the specified application is available.  This check occurs in a different
 * execution thread, permitting the original application to continue its work while the check
 * goes on in the background.  If no update is found or if any sort of error occurs, the user
 * doesn't even need to be notified; the check will silently complete (or fail) without
 * notifying the user.  Only if an actual update is found does the UpdateChecker interrupt the
 * user and ask permission to download the update.
 * 
 * The app wishing to use this library simply needs to call this during its start-up phase
 * and set it to work.  It also needs to inherit the IUpdateCheckListener interface in order
 * to receive notifications about the update check's status.  In most cases, these interface
 * methods simply pass the buck back to the UpdateChecker or perform small little tasks such
 * as storing the last update check date in whatever way the app chooses.
 * 
 * Take special note of the Debug constant.  When set to true, this forces certain errors to
 * display notification dialogs.  When using this library with a production release, this
 * flag should be set to false, which will hide these errors completely from the user.
 * 
 * UPDATES FOR VERSION 1.1:  Added the debug flag and update interval in days to the
 * constructors.  Changed the default download path to first try the value of %TEMP%, then %TMP%,
 * and finally the desktop as a last resort.  Greatly expanded the error messages so the updater
 * won't be so glaringly silent, and made sure all the dialogs generated explicitly say they're
 * coming from the Update Checker, hopefully eliminating any confusion about where they're coming
 * from.  Added a few more call-backs to the listener so it can be notified if no update was
 * found (not an error) or an error occurred during the check.  Restructured some of the public
 * members to make the private, then added public read-only properties for them.
 * 
 * This program is Copyright 2012, Jeffrey T. Darlington.
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace com.gpfcomics.UpdateChecker
{
    /// <summary>
    /// This class is the driver of the GPF Update Checker process.  Simply instantiate this
    /// class and pass it the necessary application metadata, and it will go online and search
    /// to see if a newer version of the specified application is available.
    /// </summary>
    public class UpdateChecker
    {
        #region Public Members

        /// <summary>
        /// The size in bytes of the buffer used for reading and writing data.  This is no
        /// magic figure by any means, and may get tweaked if the current number is found
        /// to be inefficient.
        /// </summary>
        public const int BufferSize = 4096;

        /// <summary>
        /// The <see cref="Uri"/> of the XML feed containing update information.
        /// </summary>
        public Uri FeedURI
        {
            get
            {
                return uri;
            }
        }

        /// <summary>
        /// If this flag is set to true, debugging will be turned on and we will get additional
        /// feedback messages.  This should always be set to false for production releases.
        /// </summary>
        public bool Debug
        {
            get
            {
                return debug;
            }
        }

        /// <summary>
        /// This is the time interval for update checks.  If the last update check was performed
        /// within this interval, another update check should not be performed.  The default
        /// value is seven days or one week.
        /// </summary>
        public TimeSpan UpdateInterval
        {
            get
            {
                return updateInterval;
            }
        }

        /// <summary>
        /// The file system path to which we'll download the update installer.  This defaults
        /// to the user's temporary files fold (the value of the %TEMP or %TMP environment
        /// variables) or to the user's destkop folder.
        /// </summary>
        public string DownloadPath
        {
            get
            {
                return downloadPath;
            }
        }

        /// <summary>
        /// A flag to determine whether or not the user should be notified if there is no
        /// update available, i.e. they have the latest version of the application.  The
        /// default is false.
        /// </summary>
        public bool NotifyOnNoUpdate
        {
            get
            {
                return notifyOnNoUpdate;
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// This is the time interval for update checks.  If the last update check was performed
        /// within this interval, another update check should not be performed.  The default
        /// value is seven days or one week.
        /// </summary>
        private static TimeSpan updateInterval = new TimeSpan(7, 0, 0, 0);

        /// <summary>
        /// If this flag is set to true, debugging will be turned on and we will get additional
        /// feedback messages.  This should always be set to false for production releases.
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// The file system path to which we'll download the update installer.  This defaults
        /// to the user's temporary files fold (the value of the %TEMP or %TMP environment
        /// variables) or to the user's destkop folder.
        /// </summary>
        private string downloadPath = null;

        /// <summary>
        /// A flag to determine whether or not the user should be notified if there is no
        /// update available, i.e. they have the latest version of the application.  The
        /// default is false.
        /// </summary>
        private bool notifyOnNoUpdate = false;

        /// <summary>
        /// This <see cref="BackgroundWorker"/> will do the grunt work of checking for updates
        /// and downloading.  It allows us to do this work in a separate thread so we don't
        /// block the user interface unless we have to.
        /// </summary>
        private BackgroundWorker worker = null;

        /// <summary>
        /// A common <see cref="WebClient"/> for downloading feeds and files
        /// </summary>
        private WebClient webClient = null;

        /// <summary>
        /// A <see cref="Uri"/> pointing to the location of the update feed to check
        /// </summary>
        private Uri uri = null;

        /// <summary>
        /// The application's unique name to help us identify it from all other apps that
        /// may use the same feed.
        /// </summary>
        private string appName = null;

        /// <summary>
        /// The current <see cref="Version"/> of the application
        /// </summary>
        private Version currentVersion = null;

        /// <summary>
        /// The <see cref="IUpdateCheckListener"/> that we will notify when an update has
        /// been found
        /// </summary>
        private IUpdateCheckListener listener = null;

        /// <summary>
        /// The date of the last update check
        /// </summary>
        private DateTime lastCheck = DateTime.MinValue;

        /// <summary>
        /// The <see cref="AppMetaData"/> read from the feed
        /// </summary>
        private AppMetaData appMetaData = null;

        /// <summary>
        /// A <see cref="ProgressDialog"/> which will display the progress of any downloads
        /// or download validations
        /// </summary>
        private ProgressDialog progressDialog = null;

        /// <summary>
        /// A string representing the full file system path to the downloaded update file
        /// </summary>
        private string downloadFile = null;

        /// <summary>
        /// A <see cref="BufferedStream"/> used for reading data, either from the Internet
        /// or from a file, depending on when it is used
        /// </summary>
        private BufferedStream inStream = null;

        /// <summary>
        /// A <see cref="BufferedStrea"/> used for writing downloaded data to a file
        /// </summary>
        private BufferedStream outStream = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// The main UpdateChecker constructor.  All fields are required.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the XML feed containing update
        /// information</param>
        /// <param name="appName">A string containing the unique name of the application so
        /// it can be identified in the feed</param>
        /// <param name="currentVersion">The current <see cref="Version"/> of the application
        /// which will be tested against any version information in the feed</param>
        /// <param name="listener">A <see cref="IUpdateCheckListener"/> who will be notified
        /// if a new update has been found</param>
        /// <param name="lastCheck">A <see cref="DateTime"/> representing the last time the
        /// update check was performed</param>
        /// <param name="intervalDays">The update check interval in days.  This must be a positive
        /// integer greater than zero.  You cannot specify an update interval less than one day.</param>
        /// <param name="debug">Whether or not to show detailed error messages (true) or simple
        /// error messages (false)</param>
        /// <exception cref="Exception">Thrown if the download path could not be initialized</exception>
        public UpdateChecker(Uri uri, string appName, Version currentVersion,
            IUpdateCheckListener listener, DateTime lastCheck, int intervalDays, bool debug)
        {
            // Get the input parameters:
            this.uri = uri;
            this.appName = appName;
            this.currentVersion = currentVersion;
            this.listener = listener;
            this.lastCheck = lastCheck;
            this.debug = debug;
            // Try to find the best place to save the download file.  We'll procced in the
            // following order:  The value of the %TEMP% environment variable, the value of
            // %TMP%, and lastly the user's desktop folder.  Try to get each one in turn.  If
            // anything goes wrong, we'll probably end up with a null or empty string.
            try
            {
                downloadPath = Environment.GetEnvironmentVariable("TEMP");
                if (String.IsNullOrEmpty(downloadPath))
                {
                    downloadPath = Environment.GetEnvironmentVariable("TMP");
                    if (String.IsNullOrEmpty(downloadPath))
                    {
                        downloadPath =
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    }
                }
            }
            catch
            {
                downloadPath = null;
            }
            // Check the value of the download path.  If it ends up being empty, null, or it doesn't
            // point to a real place, throw an exception:
            if (String.IsNullOrEmpty(downloadPath) || !Directory.Exists(downloadPath))
            {
                throw new Exception("The Update Checker was unable to find a suitable place to " +
                    "download the update file to.  Please check for updates again later.");
            }
            // If the path looks good, make sure it ends with a directory separator before we
            // attempt to use it:
            else if (!downloadPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                downloadPath += Path.DirectorySeparatorChar.ToString();
            }
            // The update check interval must a positive integer greater than one.  If it's
            // anything else, complain:
            if (intervalDays <= 0)
            {
                throw new Exception("The update check interval for the Update Checker must be " +
                    "specified in days greater than or equal to one (1).");
            }
            else
            {
                updateInterval = new TimeSpan(intervalDays, 0, 0, 0);
            }
        }

        /// <summary>
        /// An alternate constructor that allows the feed URL to be specified as a string
        /// </summary>
        /// <param name="url">A string representing the URL of the XML feed containing update
        /// information</param>
        /// <param name="appName">A string containing the unique name of the application so
        /// it can be identified in the feed</param>
        /// <param name="currentVersion">The current <see cref="Version"/> of the application
        /// which will be tested against any version information in the feed</param>
        /// <param name="listener">A <see cref="IUpdateCheckListener"/> who will be notified
        /// if a new update has been found</param>
        /// <param name="lastCheck">A <see cref="DateTime"/> representing the last time the
        /// update check was performed</param>
        /// <param name="intervalDays">The update check interval in days.  This must be a positive
        /// integer greater than zero.  You cannot specify an update interval less than one day.</param>
        /// <param name="debug">Whether or not to show detailed error messages (true) or simple
        /// error messages (false)</param>
        /// <exception cref="Exception">Thrown if the download path could not be initialized</exception>
        public UpdateChecker(string url, string appName, Version currentVersion,
            IUpdateCheckListener listener, DateTime lastCheck, int intervalDays, bool debug)
            : this(new Uri(url), appName, currentVersion, listener, lastCheck, intervalDays, debug) { }

        /// <summary>
        /// Perform the actual update check.  This method launches a special worker thread
        /// that will download and parse the XML update feed in the background.  If a newer
        /// version of the application is found, the <see cref="IUpdateCheckListener"/>'s
        /// OnFoundNewerVersion() method will be called to notify the listener of the update.
        /// If no new version can be found, or if an error occurs, the thread quietly closes.
        /// </summary>
        public void CheckForNewVersion()
        {
            // Asbestos underpants:
            try
            {
                // Check the current time verses the last update date plus the update interval.
                // If the current time is beyond the update interval, then it's time to launch
                // the update check process.
                if (lastCheck.Add(updateInterval).CompareTo(DateTime.Now) <= 0)
                {
                    // Make sure all the rest of our inputs are populated.  If they aren't,
                    // throw an exception and refuse to go any further:
                    if (uri == null || String.IsNullOrEmpty(appName) || currentVersion == null ||
                        listener == null)
                        throw new Exception("The update checker was not properly initialized.");
                    // Build and launch the background worker, which will do all the work in
                    // another thread.  Note that we won't be reporting any progress, as we're
                    // hiding that from the user at this time.  However, we need to support
                    // cancellation so we can abort the process if we hit an error.
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = false;
                    worker.WorkerSupportsCancellation = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork_UpdateCheck);
                    worker.RunWorkerCompleted +=
                        new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_UpdateCheck);
                    worker.RunWorkerAsync();
                    // Inform the listener to update their last update check date.  We don't
                    // care how they save it; we'll let them decide that detail.  But we want
                    // to make sure they record it somewhere so we don't have to do the update
                    // check too frequently.
                    listener.OnRecordLastUpdateCheck(DateTime.Now);
                }
            }
            // If anything blows up, we'll just quietly let it fail and let the main
            // application go about its business.  If debugging mode is turned on, however,
            // we'll briefly report the error before closing.
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// Ask the user whether or not they want us to download the updated application, and
        /// if so, download the installer to a temporary location and run it.  This should be
        /// called from the <see cref="IUpdateCheckListener"/>'s OnFoundNewerVersion() event
        /// handler.  Note that the listener need not implement anything else; we'll take care
        /// of asking the user if they want to get the update or not.  This has the added
        /// caveat/benefit of blocking the main app's UI when the question is asked, but
        /// returning control once the download begins.
        /// </summary>
        public void GetNewerVersion()
        {
            try
            {
                // Inform the user that the new update is available.  They may not actually
                // want the update right now, or it may be inconvenient to get it.  We should
                // always let the user decide.
                if (MessageBox.Show("A newer version of " + appName + " is available. Your " +
                    "current version is " + currentVersion.ToString() + "; the new version " +
                    "is " + appMetaData.Version.ToString() + ". Would you like to download " +
                    " this update now?", "Update Available", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Build the background worker, which will do all the work in another
                    // thread.  Note that this time, we *will* update our progress.
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.WorkerSupportsCancellation = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork_Download);
                    worker.RunWorkerCompleted +=
                        new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_Download);
                    worker.ProgressChanged +=
                        new ProgressChangedEventHandler(worker_ProgressChanged_Download);
                    // Create our progress dialog and give it a link to the worker so it
                    // can cancel it if necessary:
                    progressDialog = new ProgressDialog(worker, appMetaData.Name + " " +
                        appMetaData.Version.ToString(), ProgressDialogMode.Download);
                    progressDialog.Show();
                    // Now launch the worker and let it get to work:
                    worker.RunWorkerAsync();
                }
                // If the user decided to postpone the update, let them know they can still
                // get it from the website later:
                else MessageBox.Show("If you would like to download this update manually " +
                    "at a later time, please visit the official " + appName + " website.",
                    "Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // If anything blows up, silently exit (unless we're in debug mode, and then
            // we'll complain):
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to download the latest update. " +
                    "Please try again later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                listener.OnUpdateCheckError();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Launches the <see cref="BackgroundWorker"/> to perform the upate check.  The
        /// worker will download the XML feed, parse it, and see if a new version of the
        /// specified application exists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_DoWork_UpdateCheck(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Declare a temporary scratch app metadata object:
                AppMetaData metaData = null;
                // Create our client for fetching the feed:
                webClient = new WebClient();
                // Create our XML reader settings:
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.Schemas.Add("http://www.gpf-comics.com/",
                    Application.StartupPath +
                    Char.ToString(Path.DirectorySeparatorChar) +
                    "gpf_update_checker1.xsd");
                // Validate against the schema.  Invalid files will throw exceptions:
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.ValidationEventHandler +=
                    new ValidationEventHandler(xmlReaderSettings_ValidationEventHandler);
                // Ignore unnecessary information:
                xmlReaderSettings.IgnoreComments = true;
                xmlReaderSettings.IgnoreWhitespace = true;
                // Close any other file/input streams when we close this one:
                xmlReaderSettings.CloseInput = true;
                // Now create our XML reader, which will read data from the web client,
                // which will be downloading our feed:
                XmlReader xr = XmlReader.Create(new BufferedStream(webClient.OpenRead(uri)),
                    xmlReaderSettings);
                // Skip the header stuff and move right to the content:
                xr.MoveToContent();
                // This forces us to go to the first element, which should be <gpfupdate>.  If
                // not, complain:
                if (xr.Name != "gpfupdate")
                    throw new Exception("Invalid GPF Update Check feed file; expected <gpfupdate> tag but got <" + xr.Name + ">");
                // Read the next element.  This should be a <version> tag.  If it is, make sure
                // it's a version we recognize.  Otherwise, complain.
                xr.Read();
                if (xr.NodeType != XmlNodeType.Element)
                    throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                if (xr.Name.CompareTo("version") == 0)
                {
                    xr.Read();
                    // Make sure this is a text value, then make sure it's the version
                    // number we expect.  This version of GPF Update Check only accepts version
                    // 1 of the feed file format.
                    if (xr.NodeType == XmlNodeType.Text && xr.Value != "1")
                        throw new Exception("This GPF Update Check feed file appears to have been generated by a later version of GPF Update Check and is incompatible with this version. (File format version was " + xr.Value + ".)");
                }
                else throw new Exception("Invalid GPF Update Check feed file; expected a <version> element, but got <" + xr.Name + ">");
                // Read on to the next tag:
                xr.Read();
                while (xr.NodeType == XmlNodeType.EndElement) xr.Read();
                if (xr.NodeType != XmlNodeType.Element)
                    throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                // At this point, the next few tags should be the <generator>, <comment>, and/or
                // <pubDate> tags.  We don't care about the first two, and for now we don't
                // really care about <pubDate> either (although that may change).  Therefore,
                // just read ahead until we hit the <apps> tag, which is where we really want to
                // go to next.
                while (xr.Name.CompareTo("apps") != 0)
                {
                    do xr.Read(); while (xr.NodeType != XmlNodeType.Element);
                }
                // Now we need to check to make sure we actually got a <apps> tag:
                if (xr.NodeType == XmlNodeType.Element && xr.Name.CompareTo("apps") == 0)
                {
                    // Read the next tag.  This should be an <app> tag and the beginning of a
                    // application meta data defintion.
                    xr.Read();
                    // Declare a flag to indicate if we've found the application we're looking
                    // for.  We're only looking for one app in what could be a sea of them,
                    // so there's no point going through and parsing everything that may come
                    // after our target app.  This will let us short circuit the search.
                    bool foundIt = false;
                    // Now start stepping through the <app> tags for as long as we find them:
                    while (xr.NodeType == XmlNodeType.Element && xr.Name.CompareTo("app") == 0
                        && !foundIt)
                    {
                        // Create a new AppMetaData object as our scratch pad:
                        metaData = new AppMetaData();
                        // Try to read the <name> tag:
                        xr.Read();
                        if (xr.NodeType != XmlNodeType.Element)
                            throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                        if (xr.Name.CompareTo("name") == 0)
                        {
                            xr.Read();
                            if (xr.NodeType == XmlNodeType.Text) metaData.Name = xr.Value;
                            else throw new Exception("Invalid name token (" + xr.Value + ")");
                        }
                        else throw new Exception("Invalid GPF Update Check feed file; expected a <name> element, but got <" + xr.Name + ">");
                        do xr.Read(); while (xr.NodeType != XmlNodeType.Element);
                        // Try to read the <currentVer> tag:
                        if (xr.NodeType != XmlNodeType.Element)
                            throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                        if (xr.Name.CompareTo("currentVer") == 0)
                        {
                            xr.Read();
                            if (xr.NodeType == XmlNodeType.Text &&
                                Regex.IsMatch(xr.Value, @"^\d+\.\d+\.\d+\.\d+$"))
                                metaData.Version = new Version(xr.Value);
                            else throw new Exception("Invalid currentVer token (" + xr.Value + ")");
                        }
                        else throw new Exception("Invalid GPF Update Check feed file; expected a <currentVer> element, but got <" + xr.Name + ">");
                        do xr.Read(); while (xr.NodeType != XmlNodeType.Element);
                        // Try to read the <url> tag:
                        if (xr.NodeType != XmlNodeType.Element)
                            throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                        if (xr.Name.CompareTo("url") == 0)
                        {
                            xr.Read();
                            if (xr.NodeType == XmlNodeType.Text) metaData.Uri = new Uri(xr.Value);
                            else throw new Exception("Invalid url token (" + xr.Value + ")");
                        }
                        else throw new Exception("Invalid GPF Update Check feed file; expected a <url> element, but got <" + xr.Name + ">");
                        do xr.Read(); while (xr.NodeType != XmlNodeType.Element);
                        // Try to read the <size> tag:
                        if (xr.NodeType != XmlNodeType.Element)
                            throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                        if (xr.Name.CompareTo("size") == 0)
                        {
                            xr.Read();
                            if (xr.NodeType == XmlNodeType.Text) metaData.Size = Int64.Parse(xr.Value);
                            else throw new Exception("Invalid size token (" + xr.Value + ")");
                        }
                        else throw new Exception("Invalid GPF Update Check feed file; expected a <size> element, but got <" + xr.Name + ">");
                        do xr.Read(); while (xr.NodeType != XmlNodeType.Element);
                        // Try to read the <digest> tag:
                        if (xr.NodeType != XmlNodeType.Element)
                            throw new Exception("Invalid GPF Update Check feed file; expected an element, but got " + xr.NodeType.ToString());
                        if (xr.Name.CompareTo("digest") == 0)
                        {
                            xr.Read();
                            if (xr.NodeType == XmlNodeType.Text) metaData.Digest = xr.Value;
                            else throw new Exception("Invalid digest token (" + xr.Value + ")");
                        }
                        else throw new Exception("Invalid GPF Update Check feed file; expected a <digest> element, but got <" + xr.Name + ">");
                        // The next item should be the closing element for <digest>, so
                        // read it in and then read the next one.  That should either be the
                        // start element for the next <app> or the closing element for
                        // <apps>.
                        xr.Read();
                        if (xr.NodeType == XmlNodeType.EndElement) xr.Read();
                        else throw new Exception("Invalid GPF Update Check feed file; expected ending <digest> tag, got " + xr.NodeType.ToString());
                        if (xr.NodeType == XmlNodeType.EndElement) xr.Read();
                        else throw new Exception("Invalid GPF Update Check feed file; expected ending <app> tag, got " + xr.NodeType.ToString());
                        // We should now hopefully have a valid AppMetaData object.  Compare it
                        // to the application name we're going for, and if we find it store
                        // that for later reference.  Set the "found it" flag to true as there's
                        // no point continuing to search.
                        if (metaData.IsSameApp(appName))
                        {
                            appMetaData = metaData;
                            foundIt = true;
                        }
                    }
                    // We get here, we've exhausted the <apps> block and all that should be
                    // left will be closing tags.  If we were going to be extremely thorough,
                    // we should probably check these closing tags and make sure they're legit.
                    // For now, we'll just assume there's nothing left to read.  Close the
                    // reader and finish everything else.
                    xr.Close();
                    e.Result = foundIt ? Boolean.TrueString : Boolean.FalseString;
                }
                else throw new Exception("Invalid GPF Update Check feed file; could not find <apps> tag");
            }
            // For now, if any error occurs, just cancel the worker and ignore the error
            // (unless we're in debug mode, where we'll complain).
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to perform the update check. "
                    + "Please check for updates again later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                e.Result = Boolean.FalseString;
                worker.CancelAsync();
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// What occurs when the <see cref="BackgroundWorker"/> finishes the update check
        /// process.  If a new version was found, it notifies the listening
        /// <see cref="IUpdateCheckListener"/> so it can continue the work.  If no update
        /// was found, or if an error occurs, it will silently exist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted_UpdateCheck(object sender,
            RunWorkerCompletedEventArgs e)
        {
            try
            {
                // If we weren't cancelled and didn't run into an error, and we get got some
                // useful data from the feed, start comparing:
                if (!e.Cancelled && e.Error == null && appMetaData != null &&
                    Boolean.Parse((string)e.Result))
                {
                    // Check that we're looking at the same application and that its version
                    // is newer than the current one.  If that's the case, notify the listener
                    // and have it do the next part of its job:
                    if (appMetaData.IsSameApp(appName) &&
                        appMetaData.IsNewerVersion(currentVersion))
                        listener.OnFoundNewerVersion();
                    // If no update was found, let the listener decide whether or not the user
                    // needs to be notified:
                    else listener.OnNoUpdateFound();
                }
            }
            // We'll ignore any errors for now (unless we're in debug mode):
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to perform the update check. "
                    + "Please check for updates again later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// What to do if an XML validation event (i.e. error) occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="Exception">Thrown if any XML parsing error occurs</exception>
        void xmlReaderSettings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            // At this point, we won't be doing any error checking.  Just throw an exception,
            // which will get caught by the reader and cancel the update check.
            if (e.Severity == XmlSeverityType.Error)
                throw new Exception(e.Exception.ToString());
        }

        /// <summary>
        /// Launch the <see cref="BackgroundWorker"/> to download the file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_DoWork_Download(object sender, DoWorkEventArgs e)
        {
            try
            {
                // If for some reason the web client no longer exists, recreate it:
                if (webClient == null) webClient = new WebClient();
                // We need somewhere to safe the downloaded file.  Create a string holding the
                // path to the download folder, then tack on the last "segment" of the URI,
                // which should be the file name.
                downloadFile = downloadPath +
                    appMetaData.Uri.Segments[appMetaData.Uri.Segments.Length - 1];
                // Create two streams, one output stream to our saved file and one input
                // stream reading from the web client.  While we *could* take advantage of
                // WebClient.DownloadFile(), we can't report progress with that so we'll
                // have to read and write the file manually.
                outStream = new BufferedStream(new FileStream(downloadFile, FileMode.Create));
                inStream = new BufferedStream(webClient.OpenRead(appMetaData.Uri));
                // Create a go-between buffer for moving data from the input and output
                // streams:
                byte[] buffer = new byte[BufferSize];
                // Declare our current percent complete:
                int percentage = 0;
                int last_percent = 0;
                // We'll use this guy to keep track of how many bytes have been read from
                // the input stream in a given read.  More detail on that later.
                int bytesRead = 0;
                // Now keep reading until we've read all there is to read:
                while (true)
                {
                    // Read a block of data from the web client.  We'll try to read the entire
                    // contents of the buffer, but that might not be the case if we're on the
                    // last bit and it's smaller than the buffer size.  Thus, we'll keep track
                    // of how much we've read this time.
                    bytesRead = inStream.Read(buffer, 0, buffer.Length);
                    // If we didn't read anything at all, break out of the loop:
                    if (bytesRead == 0) break;
                    // Now write what we read to the file.  Note here that we're using the
                    // number of bytes read above; we don't want to write too much if there's
                    // extra junk at the end of the buffer.
                    outStream.Write(buffer, 0, bytesRead);
                    // Calculate our percent complete and have the worker notify the progress
                    // dialog:
                    percentage = (int)(Math.Floor((double)outStream.Position /
                        (double)appMetaData.Size * 100.00));
                    if (percentage > last_percent)
                    {
                        worker.ReportProgress(percentage);
                        last_percent = percentage;
                    }
                }
                // Flush and close the streams and move on the validation step:
                outStream.Flush();
                outStream.Close();
                inStream.Close();
                e.Result = "Success";
            }
            // If we encounter an error, cancel the worker:
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to download the latest " +
                    "update. Please try again later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                worker.CancelAsync();
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// What to do when the download process is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted_Download(object sender,
            RunWorkerCompletedEventArgs e)
        {
            try
            {
                // If we quit because of an error, report it and close up shop:
                if (e.Error != null)
                {
                    if (inStream != null) inStream.Close();
                    if (outStream != null) outStream.Close();
                    if (File.Exists(downloadFile)) File.Delete(downloadFile);
                    MessageBox.Show(e.Error.Message, "Update Check Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    listener.OnUpdateCheckError();
                }
                // Similarly, if the user cancelled the download, close up shop but remind
                // them they can download the file manually later:
                else if (e.Cancelled)
                {
                    if (inStream != null) inStream.Close();
                    if (outStream != null) outStream.Close();
                    if (File.Exists(downloadFile)) File.Delete(downloadFile);
                    MessageBox.Show("The download has been cancelled. You can always " +
                        "manually download the update later by visiting the official " +
                        appName + " website.", "Download Cancelled", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                // If the download appears to be successful:
                else if ((string)e.Result == "Success")
                {
                    // Close the progress dialog if it's still open:
                    if (progressDialog != null) progressDialog.Close();
                    // Make sure the downloaded file is present:
                    if (File.Exists(downloadFile))
                    {
                        // Create a new background worker to do the validation step:
                        worker = new BackgroundWorker();
                        worker.WorkerReportsProgress = true;
                        worker.WorkerSupportsCancellation = true;
                        worker.DoWork += new DoWorkEventHandler(worker_DoWork_Validate);
                        worker.RunWorkerCompleted +=
                            new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_Validate);
                        worker.ProgressChanged +=
                            new ProgressChangedEventHandler(worker_ProgressChanged_Download);
                        // Create our progress dialog and give it a link to the worker so it
                        // can cancel it if necessary:
                        progressDialog = new ProgressDialog(worker, appMetaData.Name + " " +
                            appMetaData.Version.ToString(), ProgressDialogMode.Verify);
                        progressDialog.Show();
                        // Now launch the worker and let it get to work:
                        worker.RunWorkerAsync();
                    }
                    // If the download file isn't present, complain:
                    else throw new Exception("Download failed.");
                }
                // If we got any other state, it's unexpected, so complain:
                else throw new Exception("Unexpected background worker completion state");
            }
            // Report any errors:
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to download the latest " +
                    "update. Please try again later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// Do the grunt work of validating the downloaded file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_DoWork_Validate(object sender, DoWorkEventArgs e)
        {
            try
            {
                // We're using SHA-256 as our validation hash, so create the engine:
                SHA256Managed hasher = new SHA256Managed();
                // Open up the downloaded file.  The buffering may not be necessary, but
                // could be useful.
                inStream = new BufferedStream(File.Open(downloadFile, FileMode.Open));
                // Declare a buffer to read into.  This is what we'll be operating on
                // as we read the file.
                byte[] buffer = new byte[BufferSize];
                // Keep track of our current percentage:
                int percent = 0;
                int last_percent = 0;
                // Keep track of the bytes read:
                int bytesRead = 0;
                long bytesSoFar = 0;
                // Loop-de-loop:
                while (true)
                {
                    // Read in a block of bytes into the buffer.  If nothing was read,
                    // break out of the loop.
                    bytesRead = inStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    // Do the hashing.  If we're at the end of the file, do the final
                    // block.  Otherwise, add this block into the pool.
                    bytesSoFar += (long)bytesRead;
                    if (bytesRead < BufferSize || bytesSoFar == inStream.Length)
                        hasher.TransformFinalBlock(buffer, 0, bytesRead);
                    else hasher.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    // Calculate the percentage complete:
                    percent = (int)(((double)bytesSoFar / (double)inStream.Length) * 100.0);
                    if (percent > last_percent)
                    {
                        // Report the status to the background worker.  Then take note of the
                        // update for the next pass.
                        if (worker != null) worker.ReportProgress(percent);
                        last_percent = percent;
                    }
                }
                // The hash is done.  Grab the final value and hold on to it:
                byte[] theHash = hasher.Hash;
                // Close the stream and the progress dialog:
                inStream.Close();
                // Now pass the hash value out to the final step:
                e.Result = Convert.ToBase64String(theHash);
            }
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to validate the downloaded " +
                    "update file. Please perform another update later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                worker.CancelAsync();
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// What to do after the download has been validated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted_Validate(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                // Close the progress dialog if it's still open:
                if (progressDialog != null) progressDialog.Close();
                // If we hit an error, close the stream (if it's open) and complain:
                if (e.Error != null)
                {
                    if (inStream != null) inStream.Close();
                    MessageBox.Show(e.Error.Message, "Update Check Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    listener.OnUpdateCheckError();
                }
                // Similarly, if we were cancelled, close the stream but warn the user that
                // we can't guarantee the file is good:
                else if (e.Cancelled)
                {
                    if (inStream != null) inStream.Close();
                    MessageBox.Show("The validation process has been cancelled. Be advised " +
                        "that since the download has not been validated, we cannot guarantee " +
                        "the file's integrity or safety. You may attempt to execute it if " +
                        "you wish, but we strongly discourage you from doing so. You will " +
                        "find the file in the following location: " + downloadFile,
                        "Validation Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    // Grab the digest:
                    string digest = (string)e.Result;
                    // Now compare its value to the value from the metadata in the feed.  If
                    // the digests match, the download was a success.  Launch the newly
                    // downloaded executable and close up shop.
                    if (appMetaData.DigestsMatch(digest))
                    {
                        MessageBox.Show("The updated version of " + appName + " has been " +
                            "successfully downloaded and is ready to install. Please save " +
                            "any work in the current version of the application. When you " +
                            "click OK, the updater will attempt to close the current version " +
                            "and launch the installer. If the current version does not close " +
                            "on its own, please close it manually before initiating the new " +
                            "install. Please note that software installation may require " +
                            "administrator privileges.", "Ready to Install", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        Process p = new Process();
                        p.StartInfo.FileName = downloadFile;
                        p.StartInfo.Verb = "Open";
                        p.Start();
                        listener.OnRequestGracefulClose();
                    }
                    // If the digests didn't match, complain:
                    else
                    {
                        MessageBox.Show("The download did not appear to be successful. Please try " +
                           "another update check later.", "Update Check Error", MessageBoxButtons.OK,
                           MessageBoxIcon.Error);
                        listener.OnUpdateCheckError();
                    }
                }
            }
            catch (Exception ex)
            {
                if (debug) MessageBox.Show(ex.ToString(), "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                else MessageBox.Show("An error occurred while trying to validate the downloaded " +
                    "update file. Please check for another update later.", "Update Check Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                listener.OnUpdateCheckError();
            }
        }

        /// <summary>
        /// What to do if the <see cref="BackgroundWorker"/> needs to report its progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_ProgressChanged_Download(object sender, ProgressChangedEventArgs e)
        {
            // The progress dialog *should* exist, but if it doesn't, we'll check to make
            // sure.  Tell it how far along we are so it can display our progress.
            if (progressDialog != null) progressDialog.UpdateProgress(e.ProgressPercentage);
        }

        #endregion
    }
}
