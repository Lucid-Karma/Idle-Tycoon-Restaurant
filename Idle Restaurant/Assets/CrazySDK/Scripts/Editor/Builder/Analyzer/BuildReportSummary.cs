using System;
using System.Collections.Generic;

namespace CrazyGames
{
    /// <summary>
    /// Summary of the build report, used by the Analyzer. Will be also serialized and stored in a file to persist across Unity restarts.
    /// </summary>
    [Serializable]
    public class BuildReportSummary
    {
        public List<PackagedFileSummary> packagedFiles = new List<PackagedFileSummary>();
        public string buildDateISO; // JSON utility does not support DateTime, so we use string
        public double durationSeconds;
        public ulong totalSize;

        /// <summary>
        /// Size of the 4 main files (wasm, framework, loader, data) in the Build directory
        /// </summary>
        public ulong initialLoadSize;
    }

    [Serializable]
    public class PackagedFileSummary
    {
        public string path;
        public ulong size;
    }
}
