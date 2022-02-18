using System.ComponentModel;
using loyal;
using Neo.SmartContract.Framework.Attributes;

namespace CSTToken
{
    [DisplayName("CSTToken")]
    [ManifestExtra("Author", "NEO")]
    [ManifestExtra("Email", "developer@neo.org")]
    [ManifestExtra("Description", "This is a CSTToken")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class CSTToken : LoyalToken
    {
        public override string Symbol() => "CST";
        public override byte Decimals() => 0;
    }
}
