using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using UEObject = UnityEngine.Object;

namespace RealisticLoans
{
    public class LoanManager : MonoBehaviour, IDisposable
    {
        private static readonly Logger logger =
            new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static LoanManager _instance;

        private static HashSet<string> _rewardMileStones = new HashSet<string>(
            new[]
            {
                "Milestone1",
                "Milestone2",
                "Milestone3",
                "Milestone4",
                "Milestone5",
                "Milestone6",
                "Milestone7",
                "Milestone8",
                "Milestone9",
                "Milestone10",
                "Milestone11",
                "Milestone12",
                "Milestone13"
            });

        public static LoanManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var previousInstance = FindObjectOfType<LoanManager>();
                if (previousInstance != null)
                {
                    previousInstance.Dispose();
                }

                var gameObject = new GameObject(nameof(LoanManager));
                _instance = gameObject.AddComponent<LoanManager>();

                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        public static void Ensure()
        {
            if (Singleton<BuildingManager>.exists)
            {
                var _ = Instance;
            }
        }

        public static void TryDispose()
        {
            if (_instance != null)
            {
                _instance.Dispose();
            }
        }

        private bool _isDisposed;
        private bool _started;
        private EconomyManager _economyManager;
        private UnlockManager _unlockManager;
        int _primeRate = 0;
        private EconomyManager.Loan[] _citiesLoans;
        private Loan[] _loans = new Loan[3];

        public LoanManager()
        {
            logger.Log($"Created LoanManager");
        }

        private bool CtrlCmdDown =>
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        private bool ShiftDown => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        private void Update()
        {
            if (_isDisposed)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.D) && this.CtrlCmdDown && this.ShiftDown)
            {
                logger.Log("Dumping UI");
                var economyPanel = UIView.GetAView()?
                    .FindUIComponent<UIPanel>("FullScreenContainer")?
                    .Find<UIPanel>("EconomyPanel");
                using (var writer = new StreamWriter("/Users/arne/git/RealisticLoans/economy.yaml"))
                {
                    Utils.DumpHierarchy(economyPanel, writer);
                }
            }

            if (Input.GetKeyDown(KeyCode.U) && this.CtrlCmdDown && this.ShiftDown)
            {
                SetLoanOffer(0, 50000, _primeRate + 1, 104);
                SetLoanOffer(1, 100000, _primeRate + 2, 520);
                SetLoanOffer(2, 200000, _primeRate + 3, 1040);
                var loanField = _economyManager.GetType().GetField(
                    "m_loans",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                var loans = (EconomyManager.Loan[]) loanField.GetValue(_economyManager);
                loans[0].m_length = 200;
            }

            if (Input.GetKeyDown(KeyCode.L) && this.CtrlCmdDown && this.ShiftDown)
            {
                for (var i = 0; i < 3; i++)
                {
                    var bankName = _economyManager.GetBankName(i);
                    logger.Log($"{bankName}");
                    EconomyManager.LoanInfo[] loanInfo;
                    if (_economyManager.GetLoanInfo(i, out loanInfo) && loanInfo.Length > 0)
                    {
                        logger.Log("loan offers");
                        foreach (var info in loanInfo)
                        {
                            loanInfo[0].m_interest = _primeRate + i;
                            logger.Log($"amount: ₡{info.m_amount:N2}, " +
                                       $"interest: {info.m_interest:N2}%, " +
                                       $"length: {info.m_length} weeks");
                        }
                    }

                    EconomyManager.Loan loan;
                    if (_economyManager.GetLoan(i, out loan))
                    {
                        logger.Log($"loan: amount: ₡{loan.m_amountLeft / 100.0:N2}/₡{loan.m_amountTaken / 100.0:N2}, " +
                                   $"interest: ₡{loan.m_interestPaid / 100.0:N2}@{loan.m_interestRate / 100.0:N2}%, " +
                                   $"length: {loan.m_length}/ weeks");
                    }
                }
            }
        }

        public void SetLoanOffer(int index, int amount, int interest, int length)
        {
            EconomyManager.LoanInfo[] loanInfo;
            if (_economyManager.GetLoanInfo(index, out loanInfo))
            {
                loanInfo[0].m_amount = amount;
                loanInfo[0].m_interest = interest;
                loanInfo[0].m_length = length;
            }
        }

        private void Start()
        {
            logger.Log("Started LoanManager");
            _unlockManager = Singleton<UnlockManager>.instance;
            _economyManager = Singleton<EconomyManager>.instance;
            _unlockManager.EventMilestoneUnlocked += DisableReward;
            _started = true;
            CheckPrimeRate();
            SetLoanOffer(0, 50000, _primeRate + 1, 104);
            SetLoanOffer(1, 100000, _primeRate + 2, 520);
            SetLoanOffer(2, 200000, _primeRate + 3, 1040);
        }

        public void Dispose()
        {
            logger.Log($"disposing");
            _unlockManager.EventMilestoneUnlocked -= DisableReward;
            Destroy(this);
            _isDisposed = true;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnDestroy()
        {
            logger.Log("destroying");
        }

        private void DisableReward(MilestoneInfo info)
        {
            if (!_rewardMileStones.Contains(info.name)) return;

            logger.Log($"Deducting {info.m_rewardCash:C0} reward for milestone " +
                       $"{info.name} from {_economyManager.LastCashAmount:C0}");
            _economyManager.AddResource(EconomyManager.Resource.RewardAmount,
                -info.m_rewardCash,
                ItemClass.Service.None,
                ItemClass.SubService.None,
                ItemClass.Level.None);
        }

        private void CheckPrimeRate()
        {
            var primeRate = 0.0;
            foreach (var t in TaxClass.All)
            {
                var taxRate = _economyManager.GetTaxRate(
                    t.Service,
                    t.SubService,
                    ItemClass.Level.None);
                primeRate += taxRate;
            }

            primeRate = Math.Floor(primeRate / TaxClass.All.Length / 2.0);
            if ((int) primeRate != _primeRate)
            {
                _primeRate = (int) primeRate;
                logger.Log($"new prime rate: {_primeRate}%");
            }
        }

        public void UpdateLoans()
        {
            if (!_started || _isDisposed) return;

            CheckPrimeRate();
            if (_citiesLoans == null)
            {
                var loanField = _economyManager.GetType().GetField(
                    "m_loans",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                _citiesLoans = (EconomyManager.Loan[]) loanField.GetValue(_economyManager);
            }

            var payments = 0;
            for (var i = 0; i < 3; i++)
            {
                if (_citiesLoans[i].m_length == 0)
                {
                    if (_loans[i] != null)
                    {
                        logger.Log($"Wiping out left over loan at index {i}");
                        _loans[i] = null;
                    }

                    continue;
                }

                if (_loans[i] == null)
                {
                    // A new loan was taken out, so we need to rewrite the loans m_length
                    // as days and initialize our loan
                    _citiesLoans[i].m_length = _citiesLoans[i].m_length * 7;
                    var loan = _loans[i] = new Loan(_citiesLoans[i]);
                    logger.Log($"Initialized new loan at index {i}: " +
                               $"amount: {loan.Amount / 100.0:C2}/{loan.AmountLeft / 100.0:C2}, " +
                               $"term: {loan.WeeksLeft} weeks, " +
                               $"weekly cost: {loan.WeeklyCost / 100.0:C2}, " +
                               $"APR: {loan.AnnualPercentageRate:P}");
                }
                else
                {
                    payments += _loans[i].MakeDailyPayment(ref _citiesLoans[i]);
                }
            }

            if (payments > 0)
            {
                
            }
        }

        private void Stuff()
        {
            var economyManager = Singleton<EconomyManager>.instance;
            for (var i = 0; i < 3; i++)
            {
                var bankName = economyManager.GetBankName(i);
                logger.Log($"{bankName}");
                EconomyManager.LoanInfo[] loanInfo;
                if (economyManager.GetLoanInfo(i, out loanInfo) && loanInfo.Length > 0)
                {
                    logger.Log("loan offers");
                    foreach (var info in loanInfo)
                    {
                        logger.Log($"amount: ₡{info.m_amount}, " +
                                   $"interest: {info.m_interest}%, " +
                                   $"length: {info.m_length} weeks");
                    }
                }

                EconomyManager.Loan loan;
                if (economyManager.GetLoan(i, out loan))
                {
                    logger.Log($"loan: amount: ₡{loan.m_amountLeft}/₡{loan.m_amountTaken}, " +
                               $"interest: ₡{loan.m_interestPaid}@{loan.m_interestRate}%, " +
                               $"length: {loan.m_length} weeks");
                }
            }

            var loanField = economyManager.GetType().GetField(
                "m_loans",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            logger.Log($"{loanField}");
            EconomyManager.Loan[] loans = (EconomyManager.Loan[]) loanField.GetValue(economyManager);
            for (var i = 0; i < 3; i++)
            {
                var bankName = economyManager.GetBankName(i);
                logger.Log($"{bankName}");
                EconomyManager.LoanInfo[] loanInfo;
                if (economyManager.GetLoanInfo(i, out loanInfo) && loanInfo.Length > 0)
                {
                    var newPercentage = loanInfo[0].m_interest + 1;
                    logger.Log($"increasing {bankName} offer percentage from {loanInfo[0].m_interest}% " +
                               $"to {newPercentage}%");
                    loanInfo[0].m_interest = newPercentage;
                }

                EconomyManager.Loan loan;
                if (economyManager.GetLoan(i, out loan))
                {
                    var newPercentage = loan.m_interestRate + 100;
                    logger.Log($"increasing {bankName} loan percentage from {loan.m_interestRate / 100.0}% " +
                               $"to {newPercentage / 100.0}%");
                    loans[i].m_interestRate = newPercentage;
                }
            }
        }
    }
}