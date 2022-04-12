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


        public Loan(EconomyManager.Loan loan, int annualPercentageRate)
        {
            AmountLeft = loan.m_amountLeft;
            Amount = loan.m_amountTaken;
            Weeks = loan.m_length;
            InterestPaid = loan.m_interestPaid;

            // We misappropriate the interest field to store payments left. It is stored as a negative value so
            // that we can distinguish between a new loan and the re-initialization after loading.
            if (loan.m_interestRate < 0)
            {
                _paymentsLeft = -1 * loan.m_interestRate;
            }
            else
            {
                // EconomyManager calculates income and expenses 16x per week and tracks the rolling
                // values in a array of 17 ints with the last value always containing the latest before
                // being moved into the week index slot
                _paymentsLeft = loan.m_length * 16;
            }

            UpdateInterestRate(annualPercentageRate);
        }

        public int MakePayment()
        {
            var periodPercentageRate = AnnualPercentageRate / 100.0 / (52 * 16);
            var interest = (int) (AmountLeft * periodPercentageRate);
            _paymentsLeft -= 1;
            int principal;
            if (_paymentsLeft > 0)
            {
                principal = Math.Min(_dailyCost - interest, AmountLeft);
            }
            else
            {
                principal = AmountLeft;
            }

            AmountLeft -= principal;
            InterestPaid += interest;

            return interest + principal;
        }

        public void UpdateInterestRate(int annualPercentageRate)
        {
            AnnualPercentageRate = annualPercentageRate;
            _dailyCost = GetLoanCost(AnnualPercentageRate, AmountLeft, _paymentsLeft, 52 * 16);
        }

        private int _paymentsLeft;
        private int _dailyCost;
        public readonly int Amount;
        public readonly int Weeks;
        public int AmountLeft { get; private set; }
        public int WeeksLeft => (int) Math.Ceiling(_paymentsLeft / 16.0);
        public int PaymentsLeft => _paymentsLeft;
        public int InterestPaid { get; private set; }
        public double AnnualPercentageRate { get; private set; }
        public int WeeklyCost => _dailyCost * 7;

        public void Persist(ref EconomyManager.Loan loan)
        {
            loan.m_interestPaid = InterestPaid;
            loan.m_interestRate = -_paymentsLeft;
            loan.m_amountLeft = AmountLeft;
            if (AmountLeft <= 0)
            {
                loan.m_length = 0;
                loan.m_amountTaken = 0;
            }
        }
    }
}