/* IUpdateCheckListener.cs
 * 
 * PROGRAMMER:    Jeffrey T. Darlington
 * DATE:          May 18, 2010
 * PROJECT:       GPFUpdateChecker
 * .NET VERSION:  2.0
 * REQUIRES:      AppMetaData
 * REQUIRED BY:   UpdateChecker
 * 
 * This interface allows the UpdateChecker class to communicate back with the calling class.
 * When an object wishes to be notified that a new version of an application has been found,
 * it just needs to inherit this interface and implement its methods.
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
using System.Windows.Forms;

namespace com.gpfcomics.UpdateChecker
{
    /// <summary>
    /// This interface allows the <see cref="UpdateChecker"/> to communicate back with the
    /// calling class.  When an object wishes to be notified that a new version of an
    /// application has been found, it just needs to inherit this interface and implement
    /// its methods.
    /// </summary>
    public interface IUpdateCheckListener
    {
        /// <summary>
        /// When a new version of an application is found, the <see cref="UpdateChecker"/>
        /// will call this method.  All implementors really need to do is call
        /// UpdateChecker.GetNewerVersion(), passing back the same <see cref="AppMetaData"/>
        /// they received from the UpdateChecker.
        /// </summary>
        void OnFoundNewerVersion();

        /// <summary>
        /// This method is called at some point during the update check process to inform
        /// the listener that they should update the last update check date to the current
        /// time.  The implementor so record this date somewhere in its settings so it will
        /// be ready on the next run.
        /// </summary>
        /// <param name="lastCheck">A <see cref="DateTime"/> representing the new value
        /// for the last update check. The implementing application should store this value
        /// and feed it back to the UpdateChecker constructor the next time the the update
        /// check should be performed.</param>
        void OnRecordLastUpdateCheck(DateTime lastCheck);

        /// <summary>
        /// This method is called by the <see cref="UpdateChecker"/> if it wants the
        /// implementor to close itself so the installer can install the new version.  This
        /// should call whatever graceful clean-up code the main program uses and ultimately
        /// close the application.
        /// </summary>
        void OnRequestGracefulClose();
    }
}
