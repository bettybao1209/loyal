package org.neo.loyaldemo.controller;

import io.neow3j.contract.SmartContract;
import io.neow3j.protocol.Neow3j;
import io.neow3j.protocol.core.response.InvocationResult;
import io.neow3j.protocol.core.response.NeoSendRawTransaction;
import io.neow3j.transaction.AccountSigner;
import io.neow3j.types.ContractParameter;
import io.neow3j.types.Hash160;
import io.neow3j.types.Hash256;
import io.neow3j.types.NeoVMStateType;
import io.neow3j.wallet.Account;
import org.neo.loyaldemo.config.Config;
import org.neo.loyaldemo.config.ContractConfig;
import org.neo.loyaldemo.pojo.OrderBook;
import org.neo.loyaldemo.pojo.OrderParams;
import org.neo.loyaldemo.service.OrderService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;


@RestController
public class OrderController {

    @Autowired
    Neow3j neow3j;
    @Autowired
    ContractConfig contractConfig;
    @Autowired
    Config config;

    @Autowired
    OrderService orderService;

    @GetMapping("/mint")
    public String mintToken(@RequestParam String symbol,  @RequestParam Hash160 account, @RequestParam Integer amount) throws Throwable {
    SmartContract smartContract = new SmartContract(new Hash160(contractConfig.getContract().get(symbol)), neow3j);
    Hash256 txHash = smartContract.invokeFunction("mintToken", ContractParameter.hash160(account), ContractParameter.integer(amount))
            .signers(AccountSigner.calledByEntry(config.ownerAccount()))
            .sign().send().getSendRawTransaction().getHash();
    return "0x" + txHash.toString();
    }

    @GetMapping("/addTokenPair")
    public String addMapToken(@RequestParam String symbolA, @RequestParam String symbolB, @RequestParam Integer ratio) throws Throwable {
        SmartContract loyalContract = new SmartContract(new Hash160(config.getLoyalContract()), neow3j);
        Hash160 tokenA = new Hash160(contractConfig.getContract().get(symbolA));
        Hash160 tokenB = new Hash160(contractConfig.getContract().get(symbolB));
        Hash256 txHash = loyalContract.invokeFunction("addTokenMap", ContractParameter.hash160(tokenA),
                ContractParameter.hash160(tokenB), ContractParameter.integer(ratio))
                .signers(AccountSigner.calledByEntry(config.ownerAccount()))
                .sign().send().getSendRawTransaction().getHash();
        return "0x" + txHash.toString();
    }

    @PostMapping("/addOrder")
    public String addOrder(@RequestBody OrderParams params) throws Throwable {
        SmartContract loyalContract = new SmartContract(new Hash160(config.getLoyalContract()), neow3j);
        Hash160 tokenA = new Hash160(contractConfig.getContract().get(params.getFromSymbol()));
        Hash160 tokenB = new Hash160(contractConfig.getContract().get(params.getToSymbol()));
        Account userAccount = Account.fromWIF(config.getWallet().get(params.getUserAddress()));
        NeoSendRawTransaction tx = loyalContract.invokeFunction("order", ContractParameter.hash160(tokenA),
                        ContractParameter.hash160(tokenB), ContractParameter.hash160(userAccount.getScriptHash()), ContractParameter.integer(params.getAmount()))
                .signers(AccountSigner.global(userAccount))
                .sign().send();

        if (tx.getResult() != null) {
            return "0x" + tx.getSendRawTransaction().getHash().toString();
        }
        else{
            return tx.getError().getMessage();
        }
    }

    @GetMapping("/getOrdersByUser")
    public List<OrderBook> getOrders(@RequestParam String address) {
        return orderService.selectOrderByAddress(address);
    }

    @GetMapping("/cancelOrder")
    public String cancelOrder(@RequestBody OrderParams params, @RequestParam String address) throws Throwable {
        SmartContract loyalContract = new SmartContract(new Hash160(config.getLoyalContract()), neow3j);

        Hash160 tokenA = new Hash160(contractConfig.getContract().get(params.getFromSymbol()));
        Hash160 tokenB = new Hash160(contractConfig.getContract().get(params.getToSymbol()));

        InvocationResult res = loyalContract.callInvokeFunction("cancelOrder",
                List.of(ContractParameter.hash160(tokenA), ContractParameter.hash160(tokenB), ContractParameter.integer(params.getOrderId())),
                AccountSigner.calledByEntry(new Hash160(address))).getInvocationResult();
        if (res.getState() == NeoVMStateType.HALT) {
            orderService.updateOrderStatus(params.getOrderId().longValue());
        }

        Account userAccount = Account.fromWIF(config.getWallet().get(address));

        NeoSendRawTransaction tx = loyalContract.invokeFunction("cancelOrder",
                ContractParameter.hash160(tokenA), ContractParameter.hash160(tokenB), ContractParameter.integer(params.getOrderId()))
                .signers(AccountSigner.calledByEntry(userAccount))
                .sign().send();

        if (tx.getResult() != null) {
            return "0x" + tx.getSendRawTransaction().getHash().toString();
        }
        else{
            return tx.getError().getMessage();
        }
    }
}
