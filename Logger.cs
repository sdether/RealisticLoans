using System;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace RealisticLoans
{
    public class Logger
    {
        private readonly Type _t;
        private readonly String _assemblyVersion;
        public IManagers managers => Singleton<SimulationManager>.instance.m_ManagersWrapper;

        public Logger(Type t)
        {
            _t = t;
            _assemblyVersion = t.Assembly.GetName().Version.ToString();
        }

        public void Log(string message)
        {
            var threading = managers.threading;
            var time = "0000-00-00 00:00:00z";
            if (threading != null)
            {
                time = managers.threading.simulationTime.ToString("u");
            }

            Debug.Log($"[RL:{_assemblyVersion}:{_t.Name}] {time} {message}");
        }
    }
}