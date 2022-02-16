using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework.Native;

namespace loyal
{
    public class LoyalProxy: SmartContract
    {
        private const byte Prefix_Pair = 0x01;
        private const byte Prefix_OwnerAddress = 0x02;
        private const byte Prefix_TokenMap = 0x03;
        private const byte Prefix_OrderId = 0x04;
        private static StorageMap Pairs => new(Storage.CurrentContext, Prefix_Pair);
        private static StorageMap TokenMap => new(Storage.CurrentContext, Prefix_TokenMap);

        public delegate void OnOrderDelegate(UInt160 fromAssetHash, UInt160 fromAddress, UInt160 toAssetHash, BigInteger amount);

        [DisplayName("Order")]
        public static event OnOrderDelegate OnOrder;

        public delegate void OnUnOrderDelegate(UInt160 from, BigInteger amount);

        [DisplayName("UnOrder")]
        public static event OnUnOrderDelegate OnUnLock;

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object _)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_TokenMap);
            Iterator<TokenPriceMap> priceMaps = (Iterator<TokenPriceMap>)tokenMap.Find(FindOptions.KeysOnly | FindOptions.RemovePrefix | FindOptions.DeserializeValues);
            while (priceMaps.Next())
            {
                TokenPriceMap tokenPriceMap = priceMaps.Value;
                UInt160 token = Runtime.CallingScriptHash;
                if (token == tokenPriceMap.FromTokenHash || token == tokenPriceMap.ToTokenHash)
                    return;
            }
            throw new Exception("Token not supported.");
        }

        public static void AddTokenMap(UInt160 fromTokenHash, UInt160 toTokenHash, BigInteger ratio)
        {
            Assert(IsOwner(), "Not authorized.");
            TokenPriceMap tokenMap = new()
            {
                FromTokenHash = fromTokenHash,
                ToTokenHash = toTokenHash
            };
            TokenMap.Put(StdLib.Serialize(tokenMap), ratio);
        }

        public static void RemoveTokenMap(UInt160 fromTokenHash, UInt160 toTokenHash)
        {
            Assert(IsOwner(), "Not authorized.");
            TokenPriceMap tokenMap = new()
            {
                FromTokenHash = fromTokenHash,
                ToTokenHash = toTokenHash
            };
            TokenMap.Delete(StdLib.Serialize(tokenMap));
        }

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

        public static bool IsOwner()
        {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_OwnerAddress });
            return Runtime.CheckWitness(owner);
        }


        [DisplayName("order")]
        public static bool Order(UInt160 fromAssetHash, UInt160 toAssetHash, UInt160 fromAddress, BigInteger amount)
        {
            Assert(amount > 0, "lock: amount SHOULD be greater than 0.");

            var Params = new object[] { fromAddress, Runtime.ExecutingScriptHash, amount };
            bool success = (bool)Contract.Call(fromAssetHash, "transfer", CallFlags.All, Params);
            Assert(success, "lock: Failed to transfer token from user to the contract.");

            byte[] tokenKey = Helper.Concat((byte[])toAssetHash, (byte[])fromAssetHash);
            var iterator = (Iterator<(ByteString, ByteString)>)Pairs.Find(tokenKey);
            foreach (var (key, value) in iterator)
            {
                BigInteger curAmount = new BigInteger(((byte[])value)[20..]);
                UInt160 user = (UInt160)((byte[])value)[..19];
                amount -= curAmount;
                if (amount >= BigInteger.Zero)
                {
                    PerformTransfer(toAssetHash, fromAddress, curAmount);
                    PerformTransfer(fromAssetHash, user, curAmount);
                    Pairs.Delete(key);
                }
                else 
                {
                    PerformTransfer(toAssetHash, fromAddress, amount + curAmount);
                    PerformTransfer(fromAssetHash, user, amount + curAmount);
                    Pairs.Put(key, -amount);
                    break;
                }
            }
            if (amount >= BigInteger.Zero)
            {
                byte[] senderKey = Helper.Concat((byte[])fromAssetHash, (byte[])toAssetHash);
                ByteString orderId = NewOrderId();
                ByteString newKey = senderKey + orderId;
                Pairs.Put(newKey, amount);
            }
            return true;
        }

        public static bool PerformTransfer(UInt160 tokenAsset, UInt160 recevier, BigInteger amount)
        {
            var fulfilParam = new object[] { Runtime.ExecutingScriptHash, recevier, recevier };
            bool fulfilSuccess1 = (bool)Contract.Call(tokenAsset, "transfer", CallFlags.All, fulfilParam);
            Assert(fulfilSuccess1, "lock: Failed to transfer token from user to the contract.");
            return true;
        }

        private static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception(msg);
        }

        private static ByteString NewOrderId()
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_OrderId };
            BigInteger id = (BigInteger)Storage.Get(context, key);
            id++;
            Storage.Put(context, key, id);
            return (ByteString)id;
        }
    }
}
