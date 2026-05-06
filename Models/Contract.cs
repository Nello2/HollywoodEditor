using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;

namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Contract
    {
        private int daysLeft;
        private int amountValue;

        [JsonIgnore]
        public bool IsInit { get; set; }

        public int contractType { get; set; }

        public int amount
        {
            get => amountValue;
            set
            {
                amountValue = value;
                startAmount = value;
            }
        }

        public int startAmount { get; set; }
        public double initialFee { get; set; }
        public double monthlySalary { get; set; }
        public double weightToSalary { get; set; }
        public DateTime dateOfSigning { get; set; }

        [JsonIgnore]
        public DateTime dateOfEnding => dateOfSigning.AddYears(amount);

        [JsonIgnore]
        public DateTime dateOfNow { get; set; }

        #region ImNotUse
        public bool is5050 { get; set; }
        public bool payed5050 { get; set; }
        public bool raiseIgn { get; set; }
        public int raiseCool { get; set; }
        public int raiseBonus { get; set; }
        public int ultimatumCool { get; set; }
        public int leaveCool { get; set; }
        public List<object> offers { get; set; }
        public object extension { get; set; }
        public bool Is5050 { get; set; }
        public double FeeWith5050 { get; set; }
        public int SecondPay { get; set; }
        #endregion

        [JsonIgnore]
        public int DaysLeft
        {
            get => daysLeft;
            set => daysLeft = value;
        }

        public void SetCalcDaysLeft()
        {
            var t = dateOfEnding;
            TimeSpan ts = t - dateOfNow;
            DaysLeft = (int)ts.TotalDays;
        }

        public static bool operator ==(Contract a, Contract b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

            return b.amount == a.amount &&
                   b.startAmount == a.startAmount &&
                   b.initialFee == a.initialFee &&
                   b.monthlySalary == a.monthlySalary &&
                   b.weightToSalary == a.weightToSalary &&
                   b.dateOfSigning == a.dateOfSigning &&
                   b.contractType == a.contractType;
        }

        public static bool operator !=(Contract a, Contract b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return this == (Contract)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + amount.GetHashCode();
                hash = hash * 23 + startAmount.GetHashCode();
                hash = hash * 23 + initialFee.GetHashCode();
                hash = hash * 23 + monthlySalary.GetHashCode();
                hash = hash * 23 + weightToSalary.GetHashCode();
                hash = hash * 23 + dateOfSigning.GetHashCode();
                hash = hash * 23 + contractType.GetHashCode();
                return hash;
            }
        }

        public Contract()
        {
            IsInit = true;
            offers = new List<object>();
            monthlySalary = 0;
            weightToSalary = 100;
        }

        public Contract(DateTime now) : this()
        {
            amount = 3;
            startAmount = 3;
            monthlySalary = 0;
            weightToSalary = 100;
            dateOfSigning = now != new DateTime() ? now.AddDays(-1) : now;
            dateOfNow = now;
            SetCalcDaysLeft();
            initialFee = 100;
            contractType = 0;

            is5050 = false;
            payed5050 = false;
            raiseIgn = false;
            raiseCool = 0;
            raiseBonus = 0;
            ultimatumCool = 0;
            leaveCool = 0;
            extension = null;
            Is5050 = false;
            FeeWith5050 = 100;
            SecondPay = 50;
            IsInit = false;
        }
    }
}