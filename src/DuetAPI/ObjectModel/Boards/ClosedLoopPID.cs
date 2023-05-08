namespace DuetAPI.ObjectModel
{
    /// <summary>
    /// Details about the closed loop PID model.
    /// </summary>
    public sealed class ClosedLoopPID : ModelObject
    {

        /// <summary>
        /// Closed Loop Acceleration feedforward
        /// </summary>
        public float A
        {
            get => _a;
            set => SetPropertyValue(ref _a, value);
        }
        private float _a;

        /// <summary>
        /// Derivative value of the Closed Loop PID
        /// </summary>
        public float D
        {
            get => _d;
            set => SetPropertyValue(ref _d, value);
        }
        private float _d;

        /// <summary>
        /// Integral value of the Closed Loop PID
        /// </summary>
        public float I
        {
            get => _i;
            set => SetPropertyValue(ref _i, value);
        }
        private float _i;

        /// <summary>
        /// Proportional value of the Closed Loop PID 
        /// </summary>
        public float P
        {
            get => _p;
            set => SetPropertyValue(ref _p, value);
        }
        private float _p;

        /// <summary>
        /// Closed Loop Velocity feedforward 
        /// </summary>
        public float V
        {
            get => _v;
            set => SetPropertyValue(ref _v, value);
        }
        private float _v;

    }
}
