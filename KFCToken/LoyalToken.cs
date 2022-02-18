using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace loyal
{
    public abstract class LoyalToken : Nep17Token
    {
        protected const byte Prefix_OwnerAddress = 0x03;
        protected const byte Prefix_Ratio = 0x04;

        [DisplayName("_deploy")]
        public static void OnDeploy(object data, bool update)
        {
            if (update) return;
            var dataList = (List<object>)data;
            var owner = (UInt160)dataList[0];
            Assert(owner != null && owner.IsValid, "Contract owner address not provided.");
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_OwnerAddress }, owner);
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Ratio }, (ByteString)dataList[1]);
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
            Mint(account, NewAmount(amount));
        }

        public static void BurnToken(UInt160 account, BigInteger amount)
        {
            Assert(IsOwner(), "Not authorized.");
            Burn(account, NewAmount(amount));
        }

        private static bool IsOwner() {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_OwnerAddress });
            return Runtime.CheckWitness(owner);
        }

        private static BigInteger NewAmount(BigInteger amount) {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_Ratio };
            BigInteger ratio = (BigInteger)Storage.Get(context, key);
            return ratio * amount;
        }
    }
}
