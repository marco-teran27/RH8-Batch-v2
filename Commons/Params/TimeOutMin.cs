using Interfaces;

namespace Commons.Params
{
    public class TimeOutMin
    {
        private static readonly TimeOutMin _instance = new TimeOutMin();
        private int _minutes = 0;

        private TimeOutMin() { }

        public static TimeOutMin Instance => _instance;

        public void SetMinutes(IConfigDataResults config)
        {
            _minutes = config.TimeoutMinutes > 0 ? config.TimeoutMinutes : 1;
        }

        public int Minutes => _minutes;
    }
}