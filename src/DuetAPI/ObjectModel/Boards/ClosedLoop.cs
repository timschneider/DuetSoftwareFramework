namespace DuetAPI.ObjectModel
{
    /// <summary>
    /// This represents information about closed-loop tuning
    /// </summary>
    public sealed class ClosedLoop : ModelObject
    {
        /// <summary>
        /// Details about Closed loop PID
        /// </summary>
        public ClosedLoopPID? PID {
            get => _pid;
            set => SetPropertyValue(ref _pid, value);
        }
        private ClosedLoopPID? _pid;

        /// <summary>
        /// Number of collected data points in the last run or 0 if it failed
        /// </summary>
        public int Points
        {
            get => _points;
            set => SetPropertyValue(ref _points, value);
        }
        private int _points;

        /// <summary>
        /// Number of completed sampling runs
        /// </summary>
        public int Runs
        {
            get => _runs;
            set => SetPropertyValue(ref _runs, value);
        }
        private int _runs;
    }
}
