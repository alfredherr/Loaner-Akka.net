using System.Collections.Generic;

namespace SnapShotStore
{
    public class Account
    {
        public Account(string accountID)
        {
            AccountID = accountID;
        }

        public string AccountID { get; }
        public string CompanyIDCustomerID { get; set; }
        public string AccountTypeID { get; set; }
        public string PrimaryAccountCodeID { get; set; }
        public int PortfolioID { get; set; }
        public string ContractDate { get; set; }
        public string DelinquencyHistory { get; set; }
        public string LastPaymentAmount { get; set; }
        public string LastPaymentDate { get; set; }
        public string SetupDate { get; set; }
        public string CouponNumber { get; set; }
        public string AlternateAccountNumber { get; set; }
        public string Desc1 { get; set; }
        public string Desc2 { get; set; }
        public string Desc3 { get; set; }
        public string ConversionAccountID { get; set; }
        public string SecurityQuestionsAnswered { get; set; }
        public string LegalName { get; set; }
        public string RandomText0 { get; set; }
        public string RandomText1 { get; set; }
        public string RandomText2 { get; set; }
        public string RandomText3 { get; set; }
        public string RandomText4 { get; set; }
        public string RandomText5 { get; set; }
        public string RandomText6 { get; set; }
        public string RandomText7 { get; set; }
        public string RandomText8 { get; set; }
        public string RandomText9 { get; set; }
        public Dictionary<string, string> LargeAccount1 { get; set; }
        public List<float> LargeAccount2 { get; set; }

        public long[] LongValues { get; set; }
        /*
                protected Account(SerializationInfo info, StreamingContext context)
                {
                    AccountID = info.GetString("AccountID");
                    CompanyIDCustomerID = info.GetString("CompanyIDCustomerID");
                    AccountTypeID = info.GetString("AccountTypeID");
                    PrimaryAccountCodeID = info.GetString("PrimaryAccountCodeID");
                    PortfolioID = info.GetInt32("PortfolioID");
                    ContractDate = info.GetString("ContractDate");
                    DelinquencyHistory = info.GetString("DelinquencyHistory");
                    LastPaymentAmount = info.GetString("LastPaymentAmount");
                    LastPaymentDate = info.GetString("LastPaymentDate");
                    SetupDate = info.GetString("SetupDate");
                    CouponNumber = info.GetString("CouponNumber");
                    AlternateAccountNumber = info.GetString("AlternateAccountNumber");
                    Desc1 = info.GetString("Desc1");
                    Desc2 = info.GetString("Desc2");
                    Desc3 = info.GetString("Desc3");
                    ConversionAccountID = info.GetString("ConversionAccountID");
                    SecurityQuestionsAnswered = info.GetString("SecurityQuestionsAnswered");
                    LegalName = info.GetString("LegalName");
                    RandomText0 = info.GetString("RandomText0");
                    RandomText1 = info.GetString("RandomText1");
                    RandomText2 = info.GetString("RandomText2");
                    RandomText3 = info.GetString("RandomText3");
                    RandomText4 = info.GetString("RandomText4");
                    RandomText5 = info.GetString("RandomText5");
                    RandomText6 = info.GetString("RandomText6");
                    RandomText7 = info.GetString("RandomText7");
                    RandomText8 = info.GetString("RandomText8");
                    RandomText9 = info.GetString("RandomText9");
                }


                public bool Equals (Account acc)
                {
                    // Compare only a few things
                    if (!AccountID.Equals(acc.AccountID)) return false;
                    if (!CouponNumber.Equals(acc.CouponNumber)) return false;
                    if (!RandomText0.Equals(acc.RandomText0)) return false;

                    return true;
                }
            }
        */
    }
}