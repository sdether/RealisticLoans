using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private int _indexLastServiced = -1;
        private FieldInfo _cashAmountField;
        private FieldInfo _cashDeltaField;
        private long[] _loanExpenses;

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
            SetLoanOffer(0, 100000, _primeRate + 1, 52);
            SetLoanOffer(1, 200000, _primeRate + 2, 260);
            SetLoanOffer(2, 400000, _primeRate + 3, 520);
            CheckLoans();
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
                for (var i = 0; i < 3; i++)
                {
                    if (_loans[i] == null) continue;
                    _loans[i].UpdateInterestRate(_primeRate);
                }
            }
        }


        public void CheckLoans()
        {
            if (!_started || _isDisposed) return;
            if (_citiesLoans == null)
            {
                _citiesLoans = (EconomyManager.Loan[]) _economyManager.GetType().GetField(
                        "m_loans",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                    )
                    ?.GetValue(_economyManager);
                _loanExpenses = (long[]) _economyManager.GetType().GetField(
                    "m_loanExpenses",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                )?.GetValue(_economyManager);
                _cashAmountField = _economyManager.GetType().GetField(
                    "m_cashAmount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                _cashDeltaField = _economyManager.GetType().GetField(
                    "m_cashDelta",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
            }

            CheckPrimeRate();
            for (var i = 0; i < 3; i++)
            {
                if (_citiesLoans[i].m_length == 0)
                {
                    if (_loans[i] != null)
                    {
                        logger.Log($"Wiping out paid off loan at index {i}");
                        _loans[i] = null;
                    }

                    continue;
                }

                if (_loans[i] == null)
                {
                    var loan = _loans[i] = new Loan(_citiesLoans[i], _primeRate + 1 + i);
                    logger.Log($"Initialized loan at index {i}: " +
                               $"amount: {loan.Amount / 100.0:C2}/{loan.AmountLeft / 100.0:C2}, " +
                               $"term: {loan.WeeksLeft} weeks/ {loan.PaymentsLeft} payment left, " +
                               $"weekly cost: {loan.WeeklyCost / 100.0:C2}, " +
                               $"APR: {loan.AnnualPercentageRate:N}%");
                }
            }
        }

        public void ServiceLoans(int index)
        {
            if (!_started || _isDisposed || index == _indexLastServiced) return;
            _indexLastServiced = index;

            CheckLoans();
            var payments = 0;
            for (var i = 0; i < 3; i++)
            {
                if (_loans[i] == null) continue;
                var loan = _loans[i];
                var payment = loan.MakePayment();
                logger.Log($"paying {payment / 100.0:C2} for week slot {index}: " +
                           $"amount: {loan.Amount / 100.0:C2}/{loan.AmountLeft / 100.0:C2}, " +
                           $"{loan.PaymentsLeft} payments left, " +
                           $"weekly cost: {loan.WeeklyCost / 100.0:C2}, " +
                           $"interest paid: {loan.InterestPaid/100.0:C2} @ " +
                           $"{loan.AnnualPercentageRate:N}%");
                payments += payment;
            }

            if (payments > 0)
            {
                logger.Log($"making loan payment: {payments / 100.0:C2}");
                _loanExpenses[16] += (long) payments;
                var cashAmount = (long) _cashAmountField.GetValue(_economyManager);
                _cashAmountField.SetValue(_economyManager, cashAmount - payments);
                var cashDelta = (long) _cashDeltaField.GetValue(_economyManager);
                _cashDeltaField.SetValue(_economyManager, cashDelta - payments);
            }
        }

        public void PersistLoans()
        {
            if (!_started || _isDisposed) return;
            CheckLoans();
            for (var i = 0; i < 3; i++)
            {
                if (_loans[i] == null) continue;
                _loans[i].Persist(ref _citiesLoans[i]);
                if (_loans[i].AmountLeft <= 0)
                {
                    logger.Log($"wiping paid off loan {i}");
                    _loans[i] = null;
                }
            }
        }
    }
}