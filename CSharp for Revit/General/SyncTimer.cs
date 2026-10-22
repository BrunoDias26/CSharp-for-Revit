using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.General
{
    /// <summary>
    /// The sync timer informs the user of total sync times.
    /// Revit calls our two methods by itself: one when a Sync with Central starts, one when it ends.
    /// </summary>
    public class SyncTimer
    {
        #region Constants

        // When the current sync started. static = one value for the whole Revit session.
        // default = "empty" date (01/01/0001), meaning "no sync started yet"
        private static DateTime SYNC_START = default;

        #endregion

        #region Registration

        /// <summary>
        /// Registers (subscribes) the sync timer to Revit's sync events. Called in OnStartup.
        /// </summary>
        /// <param name="ctlApp">The controlled application to subscribe to.</param>
        public static void Register(ControlledApplication ctlApp = null)
        {
            if (ctlApp is null)
            {
                ctlApp = Globals.CtlApp;
            }

            // += "when this event happens, also call this method" (same as _uiCltApp.Idling += RegisterUiApp)
            // ...ING = before the sync (start), ...ED = after the sync (end)
            ctlApp.DocumentSynchronizingWithCentral += DocumentSynchronizingWithCentral_SyncStart;
            ctlApp.DocumentSynchronizedWithCentral += DocumentSynchronizedWithCentral_SyncEnds;
        }

        /// <summary>
        /// Deregisters (unsubscribes) the sync timer. Called in OnShutdown.
        /// </summary>
        /// <param name="ctlApp">The controlled application to unsubscribe from.</param>
        public static void DeRegister(ControlledApplication ctlApp = null)
        {
            if (ctlApp is null)
            {
                ctlApp = Globals.CtlApp;
            }

            // -= "stop calling this method"
            ctlApp.DocumentSynchronizingWithCentral -= DocumentSynchronizingWithCentral_SyncStart;
            ctlApp.DocumentSynchronizedWithCentral -= DocumentSynchronizedWithCentral_SyncEnds;
        }

        #endregion

        #region Sync start/end

        /// <summary>
        /// Stores the time when the sync begins.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        private static void DocumentSynchronizingWithCentral_SyncStart(object sender, DocumentSynchronizingWithCentralEventArgs args)
        {
            // An error escaping from here could disturb the sync, so everything is inside try/catch
            try
            {
                SYNC_START = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SyncTimer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks the sync time when the sync ends.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        private static void DocumentSynchronizedWithCentral_SyncEnds(object sender, DocumentSynchronizedWithCentralEventArgs args)
        {
            // No start time stored: nothing to measure
            if (SYNC_START == default) { return; }

            try
            {
                SyncAssess();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SyncTimer error: {ex.Message}");
            }
        }

        #endregion

        #region Rate sync utilities

        /// <summary>
        /// Shows the sync duration and its rating.
        /// </summary>
        private static void SyncAssess()
        {
            // Now - start = how long it took (a TimeSpan)
            var elapsedTime = DateTime.Now - SYNC_START;
            var syncRating = RateSync(elapsedTime.TotalSeconds);

            // Round up to whole seconds FIRST, then split into minutes and seconds with whole numbers.
            // (Changed from the lesson: Ceiling of the remainder could show "1m 60s" instead of "2m 0s")
            int totalSeconds = (int)Math.Ceiling(elapsedTime.TotalSeconds);
            int elapsedMinutes = totalSeconds / 60;   // int / int = whole part:  125 / 60 = 2
            int excessSeconds = totalSeconds % 60;    // % = remainder:           125 % 60 = 5

            string syncTime;

            if (elapsedMinutes == 0)
            {
                syncTime = $"{excessSeconds}s";
            }
            else
            {
                syncTime = $"{elapsedMinutes}m {excessSeconds}s";
            }

            Custom.Message(title: $"Sync rating: {syncRating}",
                message: $"Duration: {syncTime}", noCancel: true);
        }

        /// <summary>
        /// Returns a rating (A to F) for a given sync time.
        /// </summary>
        /// <param name="totalSeconds">The total sync seconds.</param>
        /// <returns>A letter from A (fast) to F (slow).</returns>
        private static string RateSync(double totalSeconds)
        {
            if (totalSeconds < 60) { return "A"; }        // < 1 minute
            else if (totalSeconds < 180) { return "B"; }  // < 3 minutes
            else if (totalSeconds < 300) { return "C"; }  // < 5 minutes
            else if (totalSeconds < 600) { return "D"; }  // < 10 minutes
            else if (totalSeconds < 900) { return "E"; }  // < 15 minutes
            else { return "F"; }                          // 15 minutes or more
        }

        #endregion
    }
}
