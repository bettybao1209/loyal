using System.ComponentModel;
using loyal;
using Neo.SmartContract.Framework.Attributes;

namespace KFCToken
{
    [DisplayName("KFCToken")]
    [ManifestExtra("Author", "NEO")]
    [ManifestExtra("Email", "developer@neo.org")]
    [ManifestExtra("Description", "This is a KFCToken")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class KFTToken : LoyalToken
    {
        public override string Symbol() => "KFC";
        public override byte Decimals() => 0;
    }
}
