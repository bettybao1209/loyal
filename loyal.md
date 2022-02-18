## code structure
1. two sample Nep17-token contracts are placed in `CSTToken` and `KFCToken` directory.
2. the main contract is `LoyalOrder`， which implements the order logic and all the requirements, such as user places an order or cancel order, fulfil order, are performed on chain in a decentralized way.
3. the web service is inplements in the `loyal-demo` directory. It mainly syncs the blocks and captures the specified logs to persist the data in the db and provides services to users to view their data.

## api

### mint tokens for users

1. `http://localhost:8001/mint?symbol=kfc&account=0x201accefb2e195518663f735212a28ab445f57fb&amount=10000`
2. response: `transaction hash`.

### add token pair

To support users to swap tokens, the contract owner should add the corresponding token-pair on the contract first.


1. `http://localhost:8001/addTokenPair?symbolA=stb&symbolB=kfc&ratio=1`
2. response: `transaction hash`.

### place an order

1. `http://localhost:8001/addOrder`
2. requese body:
```
{
    "fromSymbol":"stb",
    "toSymbol":"kfc",
    "userAddress":"0x8d9a3212f25de2461227936882797f69906874c9",
    "amount":40
}
```
3. response: `transaction hash`.

### cancel the order

1. `http://localhost:8001/cancelOrder?orderId=1&address=0x8d9a3212f25de2461227936882797f69906874c9`
2. response: `transaction hash`.

### get the order

1. `http://localhost:8001/getOrdersByUser?address=fb575f44ab282a2135f763865195e1b2efcc1a20`
2. reponse body:

```
[
    {
        "orderId": 2,
        "userAddress": "fb575f44ab282a2135f763865195e1b2efcc1a20",
        "fromAssetAddress": "47659e005d5dffebc65f6f0a96749908dda4595a",
        "toAssetAddress": "a0f39735bda53ca206342487a9d09407f8b54244",
        "amount": 40,
        "createTime": 1645181170481,
        "txHash": null,
        "fulfiledAmount": 40
    }
]
```

