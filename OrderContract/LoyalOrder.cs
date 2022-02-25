using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework.Native;
using loyal;
using Neo.SmartContract.Framework.Attributes;

namespace LoyalOrder
{
    [DisplayName("LoyalOrder")]
    [ManifestExtra("Author", "NEO")]
    [ManifestExtra("Email", "developer@neo.org")]
    [ManifestExtra("Description", "This is a LoyalOrder")]
    [ContractPermission("*", "transfer")]
    public class LoyalOrder : SmartContract
    {
        private const byte Prefix_Order = 0x08;
        private const byte Prefix_OwnerAddress = 0x02;
        private const byte Prefix_TokenMap = 0x03;
        private const byte Prefix_OrderId = 0x05;
        private static StorageMap Orders => new(Storage.CurrentContext, Prefix_Order);
        private static StorageMap TokenMap => new(Storage.CurrentContext, Prefix_TokenMap);

        public delegate void OnOrderDelegate(BigInteger orderId, UInt160 fromAssetHash, UInt160 toAssetHash, UInt160 userAddresss, BigInteger amount);

        [DisplayName("Order")]
        public static event OnOrderDelegate OnOrder;

        public delegate void OnFulfilOrderDelegate(BigInteger fromOrderId, BigInteger toOrderId, BigInteger amount);

        [DisplayName("FulfilOrder")]
        public static event OnFulfilOrderDelegate FulfilOrder;

        public delegate void OnPriceMapDelegate(UInt160 fromTokenHash, UInt160 toTokenHash, BigInteger ratio);

        [DisplayName("AddPriceMap")]
        public static event OnPriceMapDelegate AddPriceMap;

        public delegate void OnDeletePriceMapDelegate(UInt160 fromTokenHash, UInt160 toTokenHash);

        [DisplayName("DeletePriceMap")]
        public static event OnDeletePriceMapDelegate DeletePriceMap;


        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object _)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_TokenMap);
            Iterator<ByteString> priceMaps = (Iterator<ByteString>)tokenMap.Find(FindOptions.KeysOnly | FindOptions.RemovePrefix);
            while (priceMaps.Next())
            {
                TokenPair tokenPriceMap = (TokenPair)StdLib.Deserialize(priceMaps.Value);
                UInt160 token = Runtime.CallingScriptHash;
                if (token == tokenPriceMap.FromTokenHash || token == tokenPriceMap.ToTokenHash)
                    return;
            }
            throw new Exception("Token not supported.");
        }

        public static void AddTokenMap(UInt160 fromTokenHash, UInt160 toTokenHash, BigInteger ratio)
        {
            Assert(IsOwner(), "Not authorized.");
            TokenPair tokenMap = new()
            {
                FromTokenHash = fromTokenHash,
                ToTokenHash = toTokenHash
            };
            TokenMap.Put(StdLib.Serialize(tokenMap), ratio);
            AddPriceMap(fromTokenHash, toTokenHash, ratio);
        }

        public static void RemoveTokenMap(UInt160 fromTokenHash, UInt160 toTokenHash)
        {
            Assert(IsOwner(), "Not authorized.");
            TokenPair tokenMap = new()
            {
                FromTokenHash = fromTokenHash,
                ToTokenHash = toTokenHash
            };
            TokenMap.Delete(StdLib.Serialize(tokenMap));
            DeletePriceMap(fromTokenHash, toTokenHash);
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
            var Params = new object[] { fromAddress, Runtime.ExecutingScriptHash, amount, null };
            bool success = (bool)Contract.Call(fromAssetHash, "transfer", CallFlags.All, Params);
            Assert(success, "lock: Failed to transfer token from user to the contract.");

            byte[] tokenKey = CreateOrder(fromAssetHash, toAssetHash, fromAddress, amount);

            byte[] reverseTokenKey = Helper.Concat((byte[])toAssetHash, (byte[])fromAssetHash);

            var iterator = (Iterator<(ByteString, ByteString)>)Orders.Find(reverseTokenKey);
            BigInteger transferToFrom = 0;
            BigInteger amountClone = amount;

            foreach (var (key, value) in iterator)
            {
                var newKey = ((byte[])key)[1..];
                BigInteger curAmount = new(((byte[])value)[20..]);
                UInt160 user = (UInt160)((byte[])value)[..20];

                BigInteger toOrderId = new(newKey[40..]);
                
                BigInteger fromOrderId = new(tokenKey[40..]);
                if (amount >= curAmount)
                {
                    transferToFrom += curAmount;
                    PerformTransfer(fromAssetHash, user, curAmount);
                    Orders.Delete(newKey);
                    FulfilOrder(fromOrderId, toOrderId, curAmount);
                    if (amount == curAmount)
                    {
                        Orders.Delete(tokenKey);
                        break;
                    }
                    amount -= curAmount;
                }
                else
                {
                    transferToFrom += amount;
                    PerformTransfer(fromAssetHash, user, amount);
                    ByteString newValue = user + (ByteString)(curAmount - amount);
                    Orders.Put(newKey, newValue);
                    Orders.Delete(tokenKey);
                    FulfilOrder(fromOrderId, toOrderId, amount);
                    break;
                }
            }

            PerformTransfer(toAssetHash, fromAddress, transferToFrom);
            if (transferToFrom < amountClone)
            {
                ByteString newValue = fromAddress + (ByteString)(amountClone - transferToFrom);
                Orders.Put(tokenKey, newValue);
            }
            return true;
        }

        public static bool CancelOrder(UInt160 fromToken, UInt160 toToken, BigInteger orderId)
        {
            byte[] orderKey = GetTokenKey(fromToken, toToken, orderId);
            byte[] userAmount = (byte[])Orders.Get(orderKey);
            Assert(userAmount != null, "Order not exists.");
            UInt160 user = (UInt160)userAmount[..20];
            BigInteger amount = new(userAmount[20..]);
            Assert(Runtime.CheckWitness(user), "Not authorized.");
            Orders.Delete(orderKey);
            PerformTransfer(fromToken, user, amount);
            return true;
        }

        private static bool PerformTransfer(UInt160 tokenAsset, UInt160 recevier, BigInteger amount)
        {
            var fulfilParam = new object[] { Runtime.ExecutingScriptHash, recevier, amount, null };
            bool fulfilSuccess = (bool)Contract.Call(tokenAsset, "transfer", CallFlags.All, fulfilParam);
            Assert(fulfilSuccess, "lock: Failed to transfer" + tokenAsset.ToAddress(0x53) + "from the contract to the " + recevier.ToAddress(0x53));
            return true;
        }

        private static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception(msg);
        }

        private static BigInteger NewOrderId()
        {
            StorageContext context = Storage.CurrentContext;
            byte[] key = new byte[] { Prefix_OrderId };
            BigInteger id = (BigInteger)Storage.Get(context, key);
            id++;
            Storage.Put(context, key, id);
            return id;
        }

        private static byte[] GetTokenKey(ByteString fromToken, ByteString toToken, BigInteger orderId)
        {
            byte[] token = Helper.Concat((byte[])fromToken, (byte[])toToken);
            return Helper.Concat(token, (ByteString)orderId);
        }

        private static byte[] CreateOrder(UInt160 fromAssetHash, UInt160 toAssetHash, UInt160 userAddress, BigInteger amount)
        {
            BigInteger orderId = NewOrderId();
            byte[] tokenKey = GetTokenKey(fromAssetHash, toAssetHash, orderId);

            ByteString newValue = userAddress + (ByteString)amount;
            Orders.Put(tokenKey, newValue);

            byte[] userToken = Helper.Concat((byte[])fromAssetHash, (byte[])userAddress);
            OnOrder(orderId, fromAssetHash, toAssetHash, userAddress, amount);
            return tokenKey;
        }
    }
}
