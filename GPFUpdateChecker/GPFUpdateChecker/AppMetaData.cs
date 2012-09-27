/* AppMetaData.cs
 * 
 * PROGRAMMER:    Jeffrey T. Darlington
 * DATE:          May 18, 2010
 * PROJECT:       GPFUpdateChecker
 * .NET VERSION:  2.0
 * REQUIRES:      (None)
 * REQUIRED BY:   UpdateChecker, IUpdateCheckListener
 * 
 * This class represents application metadata read from the GPF Update Checker XML format.
 * It encapsulates the raw data read from the feed, as well as provides a number of convenience
 * methods for testing data against other values.
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
using System.Text;

namespace com.gpfcomics.UpdateChecker
{
    /// <summary>
    /// This class represents application metadata read from the GPF Update Checker XML
    /// format.  It encapsulates the raw data read from the feed, as well as provides a
    /// number of convenience methods for testing data against other values.
    /// </summary>
    public class AppMetaData
    {
        #region Private Members

        /// <summary>
        /// The name of the application.  This should be unique for all applications within
        /// the feed, and will be used as the primary token for identifying the app from
        /// other apps.
        /// </summary>
        private string name = null;

        /// <summary>
        /// The <see cref="Version"/> of the application as returned by the feed.
        /// </summary>
        private Version version = null;

        /// <summary>
        /// A <see cref="Uri"/> that points to where the version of the application represented
        /// in the feed can be downloaded.
        /// </summary>
        private Uri uri = null;

        /// <summary>
        /// The size of the download file in bytes.
        /// </summary>
        private long size = -1L;

        /// <summary>
        /// A Base64-encoded string containing the SHA-256 hash of the downloadable installer,
        /// located at the URL specified.  Compare this value to a hash generated from the
        /// downloaded file to ensure that the download was successful.
        /// </summary>
        private string digest = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// The name of the application.  This should be unique for all applications within
        /// the feed, and will be used as the primary token for identifying the app from
        /// other apps.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The <see cref="Version"/> of the application as returned by the feed.
        /// </summary>
        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        /// <summary>
        /// A <see cref="Uri"/> that points to where the version of the application represented
        /// in the feed can be downloaded.
        /// </summary>
        public Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        /// <summary>
        /// The size of the download file in bytes.
        /// </summary>
        public long Size
        {
            get { return size; }
            set { size = value; }
        }

        /// <summary>
        /// A Base64-encoded string containing the SHA-256 hash of the downloadable installer,
        /// located at the URL specified.  Compare this value to a hash generated from the
        /// downloaded file to ensure that the download was successful.
        /// </summary>
        public string Digest
        {
            get { return digest; }
            set { digest = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The base AppMetaData constructor.  This creates an empty AppMetaData object with
        /// nothing intitialized.  This is primarily intended for creating an AppMetaData
        /// object on the fly while it is being read by the XML parser.
        /// </summary>
        public AppMetaData() { }

        /// <summary>
        /// The full AppMetaData constructor, taking all the internal data in one gulp
        /// </summary>
        /// <param name="name">The application name</param>
        /// <param name="version">The application <see cref="Version"/></param>
        /// <param name="uri">The download <see cref="Uri"/></param>
        /// <param name="size">The size of the download file in bytes.</param>
        /// <param name="digest">A Base64-encoded SHA-256 digest of the download file</param>
        public AppMetaData(string name, Version version, Uri uri, long size, string digest)
        {
            this.name = name;
            this.version = version;
            this.uri = uri;
            this.size = size;
            this.digest = digest;
        }

        /// <summary>
        /// Compare the app name from the metadata with another app name to see if they
        /// refer to the same application
        /// </summary>
        /// <param name="otherName">The name of the application to test</param>
        /// <returns>True if both names refer to the same application, false otherwise</returns>
        public bool IsSameApp(string otherName)
        {
            if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(otherName))
                return name.CompareTo(otherName) == 0;
            else return false;
        }

        /// <summary>
        /// Compare the <see cref="Version"/> of the application in the metadata with Version
        /// data from somewhere (like an assembly's metadata) to see if the feed Version is
        /// later and thus newer than the current one.
        /// </summary>
        /// <param name="oldVersion">The current <see cref="Version"/> to test against</param>
        /// <returns>True if the feed represents a newer version, false otherwise</returns>
        public bool IsNewerVersion(Version oldVersion)
        {
            if (version != null && oldVersion != null)
                return version.CompareTo(oldVersion) > 0;
            else return false;
        }

        /// <summary>
        /// Compare the digest string of the application with a digest computed from a
        /// downloaded file and see if the two match
        /// </summary>
        /// <param name="digestToCheck">A Base64-encoded SHA-256 digest as a string</param>
        /// <returns>True if the two digests match, false otherwise</returns>
        public bool DigestsMatch(string digestToCheck)
        {
            if (!String.IsNullOrEmpty(digest) && !String.IsNullOrEmpty(digestToCheck))
                return digest.CompareTo(digestToCheck) == 0;
            else return false;
        }

        #endregion

    }
}
