using ICities;
using System;
namespace RealisticLoans
{
    public class Economy : EconomyExtensionBase
    {
        private static Logger logger = new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override void OnReleased()
        {
            logger.Log($"released economy");
            LoanManager.TryDispose();
            base.OnReleased();
        }
    }
}