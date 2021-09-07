using QUIKSharp;
using System.Linq;

namespace QuikSharp.Tests.Helpers
{
    class TestQ : ITradeSecurity, ISecurity
    {
        public string ClientCode = string.Empty;
        public string FirmId = string.Empty;
        public string Tag;
        public string AccountID = string.Empty;
        public string ClassCode = "SPBFUT";
        public string SecCode = "VBM1";

        string ITrader.AccountID => AccountID;
        string ITrader.ClientCode => ClientCode;

        string ISecurity.ClassCode => ClassCode;

        string ISecurity.SecCode => SecCode;

        string ITrader.FirmId => FirmId;

        public TestQ(IQuik q, string ClassCode = null, string SecCode = null)
        {
            var money_limits = q.Class.GetMoneyLimits().ConfigureAwait(false).GetAwaiter().GetResult();
            var depo_limits = q.Class.GetDepoLimits().ConfigureAwait(false).GetAwaiter().GetResult();

            var fst = money_limits.FirstOrDefault();
            ClientCode = fst?.ClientCode;
            FirmId = fst?.FirmId;
            Tag = fst?.Tag;
            AccountID = depo_limits.Where(row => row.TrdAccId != string.Empty).FirstOrDefault()?.TrdAccId;

            if (!string.IsNullOrEmpty(ClassCode))
            {
                this.ClassCode = ClassCode;
            }
            if (!string.IsNullOrEmpty(SecCode))
            {
                this.SecCode = SecCode;
            }
        }
    }
}
