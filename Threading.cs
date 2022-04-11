using System;
using ICities;

namespace RealisticLoans
{
    public class Threading : ThreadingExtensionBase
    {
        private static Logger logger = new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private DayOfWeek _lastDay = DayOfWeek.Monday;
        public override void OnBeforeSimulationFrame()
        {
            if (_lastDay != threadingManager.simulationTime.DayOfWeek)
            {
                _lastDay = threadingManager.simulationTime.DayOfWeek;
                logger.Log($"{_lastDay}");
                LoanManager.Instance.UpdateLoans();
            }
        }

      
    }
}