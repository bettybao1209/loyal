using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace loyal
{
    [DisplayName("loyal")]
    [ManifestExtra("Author", "NEO")]
    [ManifestExtra("Email", "developer@neo.org")]
    [ManifestExtra("Description", "This is a loyal")]
    public abstract class LoyalToken : Nep17Token
    {
        protected const byte Prefix_OwnerAddress = 0x01;
        
        [DisplayName("_deploy")]
        public static void OnDeploy(object data, bool update)
        {
            if (update) return;
            var owner = (UInt160)data;
            Assert(owner != null && owner.IsValid, "Contract owner address not provided.");
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_OwnerAddress }, owner);
        }

        public static void Update(ByteString nefFile, string manifest, object data)
        {
            Assert(IsOwner(), "Not authorized.");
            ContractManagement.Update(nefFile, manifest, data);
        }

        private static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception(msg);
        }

        public static void MintToken(UInt160 account, BigInteger amount)
        {
            Assert(IsOwner(), "Not authorized.");
            Mint(account, newAmount(amount));
        }

        public static void BurnToken(UInt160 account, BigInteger amount)
        {
            Assert(IsOwner(), "Not authorized.");
            Burn(account, newAmount(amount));
        }

        private static bool IsOwner() {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_OwnerAddress });
            return Runtime.CheckWitness(owner);
        }

        private static BigInteger newAmount(BigInteger amount) {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_Ratio };
            BigInteger ratio = (BigInteger)Storage.Get(context, key);
            return ratio * amount;
        }
    }
}
