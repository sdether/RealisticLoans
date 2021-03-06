using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using HarmonyLib;
using UnityEngine;

namespace RealisticLoans
{
    public class Patcher
    {
        private const string HarmonyId = "sdether.RealisticLoans";
        private static bool _patched;

        public static void PatchAll()
        {
            if (_patched) return;

            _patched = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Patcher).Assembly); // you can also do manual patching here!
        }

        public static void UnpatchAll()
        {
            if (!_patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            _patched = false;
        }
    }

    [HarmonyPatch(typeof(EconomyPanel), "PopulateLoansTab")]
    public static class EconomyPanelPopulateLoansTabPatch
    {
        private static readonly Logger logger =
            new Logger(MethodBase.GetCurrentMethod().DeclaringType);

        private static bool IsLoanUnlocked(int i)
        {
            switch (i)
            {
                case 0:
                    return ToolsModifierControl.IsUnlocked(UnlockManager.Feature.Loans);
                case 1:
                    return ToolsModifierControl.IsUnlocked(UnlockManager.Feature.SecondLoan);
                case 2:
                    return ToolsModifierControl.IsUnlocked(UnlockManager.Feature.ThirdLoan);
                default:
                    return false;
            }
        }

        private static string GetUnlockText(int i)
        {
            if (Singleton<UnlockManager>.exists && i >= 0 && i < 3)
            {
                UnlockManager.Feature index = UnlockManager.Feature.Loans;
                if (i == 0)
                    index = UnlockManager.Feature.Loans;
                if (i == 1)
                    index = UnlockManager.Feature.SecondLoan;
                if (i == 2)
                    index = UnlockManager.Feature.ThirdLoan;
                MilestoneInfo featureMilestone =
                    Singleton<UnlockManager>.instance.m_properties.m_FeatureMilestones[(int) index];
                if (!ToolsModifierControl.IsUnlocked(featureMilestone))
                    return "<color #f4a755>" + featureMilestone.GetLocalizedProgress().m_description + "</color>";
            }

            return string.Empty;
        }

        public static bool Prefix(EconomyPanel __instance)
        {
            for (int index = 0; index < 3; ++index)
            {
                UIPanel uiPanel = __instance.Find<UIPanel>("LoanOffer" + index);
                uiPanel.isEnabled = IsLoanUnlocked(index);
                uiPanel.tooltip = GetUnlockText(index);
                UIButton uiButton = uiPanel.Find<UIButton>("Action");
                UILabel uiLabel = uiPanel.Find<UILabel>("BankLabel");
                if (Singleton<EconomyManager>.exists)
                {
                    string bankName = Singleton<EconomyManager>.instance.GetBankName(index);
                    uiPanel.isVisible = !string.IsNullOrEmpty(bankName);
                    if (!string.IsNullOrEmpty(bankName))
                    {
                        uiLabel.text = Locale.Get("BANKNAME", bankName);
                        EconomyManager.Loan loan;
                        bool flag;
                        if (Singleton<EconomyManager>.instance.GetLoan(index, out loan))
                        {
                            flag = true;
                            uiPanel.color = __instance.m_TakenLoanColor;
                            uiButton.text = Locale.Get("LOAN_PAY");
                            uiButton.isEnabled =
                                Singleton<EconomyManager>.instance.PeekResource(EconomyManager.Resource.LoanAmount,
                                    loan.m_amountLeft) == loan.m_amountLeft;
                        }
                        else
                        {
                            flag = false;
                            uiPanel.color = __instance.m_AvailableLoanColor;
                            uiButton.text = Locale.Get("LOAN_TAKE");
                            uiButton.isEnabled = true;
                        }

                        EconomyManager.LoanInfo[] infos;
                        if (Singleton<EconomyManager>.instance.GetLoanInfo(index, out infos))
                        {
                            UIComponent uiComponent1 = uiPanel.Find("OfferInfoDesc");
                            UIComponent uiComponent2 = uiPanel.Find("OfferInfo");
                            uiComponent1.Find<UILabel>("Info6").isVisible = true;
                            uiComponent2.Find<UILabel>("Info6").isVisible = true;
                            if (!flag)
                            {
                                var weeklyCost = Loan.GetLoanCost(infos[0]);
                                var loanTotal = weeklyCost * infos[0].m_length;
                                var interest = loanTotal - infos[0].m_amount;
                                uiComponent1.Find<UILabel>("Info1").text = Locale.Get("LOAN_AMOUNT");
                                uiComponent2.Find<UILabel>("Info1").text = infos[0].m_amount
                                    .ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info2").text = Locale.Get("LOAN_PAYMENTPLAN");
                                uiComponent2.Find<UILabel>("Info2").text = StringUtils.SafeFormat(
                                    Locale.Get("LOAN_PAYMENTFORMAT"),
                                    infos[0].m_length,
                                    infos[0].m_length != 1
                                        ? Locale.Get("DATETIME_WEEKS")
                                        : (object) Locale.Get("DATETIME_WEEK"));

                                uiComponent1.Find<UILabel>("Info3").text = Locale.Get("LOAN_INTEREST");
                                uiComponent2.Find<UILabel>("Info3").text = StringUtils.SafeFormat(
                                    Locale.Get("VALUE_PERCENTAGE"),
                                    infos[0].m_interest);
                                uiComponent1.Find<UILabel>("Info4").text = Locale.Get("LOAN_WEEKLYCOST");
                                uiComponent2.Find<UILabel>("Info4").text =
                                    weeklyCost.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info5").text = Locale.Get("LOAN_TOTAL");
                                uiComponent2.Find<UILabel>("Info5").text =
                                    loanTotal.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info6").text = "Interest";
                                uiComponent2.Find<UILabel>("Info6").text =
                                    interest.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                            }
                            else
                            {
                                var loan2 = LoanManager.Instance.GetLoan(index);
                                var amountLeft = loan2.AmountLeft / 100f;
                                var loanTotal = (loan2.WeeklyCost / 100f) * loan2.Weeks;
                                var weeklyCost = loan2.WeeklyCost / 100f;
                                var interestPaid = loan2.InterestPaid / 100f;
                                uiComponent1.Find<UILabel>("Info1").text = Locale.Get("LOAN_PAYMENTLEFT");
                                uiComponent2.Find<UILabel>("Info1").text = amountLeft.ToString(Settings.moneyFormat,
                                    LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info2").text = Locale.Get("LOAN_PAYMENTTIMELEFT");
                                uiComponent2.Find<UILabel>("Info2").text = StringUtils.SafeFormat(
                                    Locale.Get("LOAN_PAYMENTFORMAT"),
                                    loan2.WeeksLeft,
                                    loan2.WeeksLeft != 1
                                        ? Locale.Get("DATETIME_WEEKS")
                                        : (object) Locale.Get("DATETIME_WEEK"));
                                uiComponent1.Find<UILabel>("Info3").text = Locale.Get("LOAN_INTEREST");
                                uiComponent2.Find<UILabel>("Info3").text = StringUtils.SafeFormat(
                                    Locale.Get("VALUE_PERCENTAGE"), loan2.AnnualPercentageRate);
                                uiComponent1.Find<UILabel>("Info4").text = Locale.Get("LOAN_WEEKLYCOST");
                                uiComponent2.Find<UILabel>("Info4").text =
                                    weeklyCost.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info5").text = "Interest Paid";
                                uiComponent2.Find<UILabel>("Info5").text =
                                    interestPaid.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                                uiComponent1.Find<UILabel>("Info6").text =
                                    Locale.Get("LOAN_TOTAL");
                                uiComponent2.Find<UILabel>("Info6").text =
                                    loanTotal.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}