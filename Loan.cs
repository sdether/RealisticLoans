using System;
using Epic.OnlineServices.Ecom;

namespace RealisticLoans
{
    public class Loan
    {
        public static int GetLoanCost(EconomyManager.LoanInfo info)
        {
            return GetLoanCost(
                info.m_interest,
                info.m_amount,
                info.m_length,
                52
            );
        }

        private static int GetLoanCost(double annualPercentageRate, int amount, int payments, int timefactor)
        {
            var periodInterestRate = annualPercentageRate / 100.0 / timefactor;
            var power = Math.Pow(1 + periodInterestRate, payments);
            var payment = amount * periodInterestRate * power / (power - 1);
            return (int) Math.Round(payment, 0);
        }


        public Loan(EconomyManager.Loan loan)
        {
            AmountLeft = loan.m_amountLeft;
            Amount = loan.m_amountTaken;
            // We misappropriate the term length from the existing loan structure to store
            // our days left on the loan, so that it gets serialized into the savegame
            _daysLeft = loan.m_length;
            AnnualPercentageRate = loan.m_interestRate / 100.0;
            InterestPaid = loan.m_interestPaid;
            SetDailyCost();
        }

        private void SetDailyCost()
        {
            _dailyCost = GetLoanCost(AnnualPercentageRate, Amount, _daysLeft, 52 * 7);
        }

        public int MakeDailyPayment(ref EconomyManager.Loan loan)
        {
            var dailyPercentageRate = AnnualPercentageRate / 100.0 / (52 * 7);
            var interest = (int) (AmountLeft * dailyPercentageRate);
            _daysLeft -= 1;
            int principal;
            if (_daysLeft < 0)
            {
                principal = Math.Min(_dailyCost - interest, AmountLeft);
            }
            else
            {
                principal = AmountLeft;
            }

            AmountLeft -= principal;
            InterestPaid += interest;

            loan.m_length = _daysLeft;
            loan.m_length = InterestPaid;
            loan.m_interestRate = (int) AnnualPercentageRate * 100;
            loan.m_amountLeft = AmountLeft;
            return interest + principal;
        }

        public void UpdateInterestRate(int annualPercentageRate)
        {
            AnnualPercentageRate = annualPercentageRate;
            SetDailyCost();
        }

        private int _daysLeft;
        private int _dailyCost;
        public int Amount { get; private set; }
        public int AmountLeft { get; private set; }
        public int WeeksLeft => (int) Math.Ceiling(_daysLeft / 7.0);
        public int InterestPaid { get; private set; }
        public double AnnualPercentageRate { get; private set; }
        public int WeeklyCost => _dailyCost * 7;
    }
}