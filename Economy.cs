using ICities;
using System;
using ColossalFramework;

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

        public override int OnPeekResource(EconomyResource resource, int amount)
        {
            if (resource == EconomyResource.LoanPayment)
            {
                return 0;
            }

            return base.OnPeekResource(resource, amount);
        }

        public override int OnFetchResource(EconomyResource resource, int amount, Service service,
            SubService subService, Level level)
        {
            if (resource == EconomyResource.LoanPayment)
            {
                uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                uint index1 = currentFrameIndex >> 8 & 15U;
                if ((currentFrameIndex & (uint) byte.MaxValue) == (uint) byte.MaxValue)
                {
                    LoanManager.Instance.ServiceLoans((int) index1);
                }

                return 0;
            }
            return base.OnFetchResource(resource, amount, service, subService, level);
        }

        public override long OnUpdateMoneyAmount(long internalMoneyAmount)
        {
            LoanManager.Instance.PersistLoans();
            return internalMoneyAmount;
        }
    }
}