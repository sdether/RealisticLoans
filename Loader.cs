using System;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace RealisticLoans
{
    public class Loader : ILoadingExtension
    {
        private static Logger logger = new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCreated(ILoading loading)
        {
            logger.Log($"OnCreated - mode: {loading.currentMode}, complete: {loading.loadingComplete}");
            if (loading.currentMode == AppMode.Game && loading.loadingComplete)
            {
                LoanManager.Ensure();
            }
        }

        public void OnReleased()
        {
            logger.Log("OnReleased");
            LoanManager.TryDispose();
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            logger.Log($"OnLevelLoaded - mode: {mode}");
            LoanManager.Ensure();
        }

        public void OnLevelUnloading()
        {
            logger.Log("OnLevelUnloading");
            LoanManager.TryDispose();
        }
    }
}