package org.neo.loyaldemo.chain;

import io.neow3j.protocol.Neow3j;
import io.neow3j.protocol.core.response.*;
import io.neow3j.protocol.core.stackitem.StackItem;
import io.neow3j.types.NeoVMStateType;
import org.neo.loyaldemo.config.Config;
import org.neo.loyaldemo.pojo.FulfilRecord;
import org.neo.loyaldemo.pojo.OrderBook;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Component
public class CommonService {

    @Autowired
    private Neow3j neow3j;
    @Autowired
    Config config;

    public BigInteger getRemoteBlockIndex() throws IOException {
        NeoBlockCount blockCount = neow3j.getBlockCount().send();
        BigInteger blockIndex = blockCount.getBlockCount().subtract(BigInteger.valueOf(1));
        return blockIndex;
    }

    public NeoBlock getBlock(BigInteger blockIndex) throws IOException {
        NeoGetBlock block = neow3j.getBlock(blockIndex, true).send();
        return block.getBlock();
    }

    public Map<String, Object> getOrderBooks(Transaction tx, long blockTime) throws IOException{

        NeoApplicationLog logs = neow3j.getApplicationLog(tx.getHash()).send().getApplicationLog();
        List<OrderBook> orderBooks = new ArrayList<>();
        List<FulfilRecord> fulfilRecords = new ArrayList<>();
        if(logs != null){
            List<NeoApplicationLog.Execution> executions = logs.getExecutions();
            if (executions.get(0).getState() != NeoVMStateType.HALT) {
                return new HashMap<>();
            }
            executions.parallelStream().flatMap(execution -> execution.getNotifications().stream())
                .forEach(notification -> {
                    if (config.getLoyalContract().equals("0x" + notification.getContract())){
                        List<StackItem> state = (ArrayList<StackItem>) notification.getState().getValue();
                        if ("Order".equals(notification.getEventName())) {
                            OrderBook orderBook = new OrderBook();
                            orderBook.setOrderId((state.get(0)).getValue() == null ? BigInteger.valueOf(-1) : state.get(0).getInteger());
                            orderBook.setFromAssetAddress((state.get(1)).getValue() == null ? "" : state.get(1).getHexString());
                            orderBook.setToAssetAddress((state.get(2)).getValue() == null ? "" : state.get(2).getHexString());
                            orderBook.setUserAddress((state.get(3)).getValue() == null ? "" : state.get(3).getHexString());
                            orderBook.setAmount((state.get(4)).getValue() == null ? BigInteger.valueOf(-1) : state.get(4).getInteger());
                            orderBook.setTxHash("0x" + tx.getHash().toString());
                            orderBook.setCreateTime(blockTime);
                            orderBooks.add(orderBook);
                        }
                        else if ("FulfilOrder".equals(notification.getEventName())) {
                            FulfilRecord fulfilRecord = new FulfilRecord();
                            fulfilRecord.setOrderIdFrom((state.get(0)).getValue() == null ? BigInteger.valueOf(-1) : state.get(0).getInteger());
                            fulfilRecord.setOrderIdTo((state.get(1)).getValue() == null ? BigInteger.valueOf(-1) : state.get(1).getInteger());
                            fulfilRecord.setAmount((state.get(2)).getValue() == null ? BigInteger.valueOf(-1) : state.get(2).getInteger());
                            fulfilRecord.setTxHash("0x" + tx.getHash().toString());
                            fulfilRecord.setCreateTime(blockTime);
                            fulfilRecords.add(fulfilRecord);
                        }
                    }
                });
        }
        Map<String, Object> res = new HashMap<>();
        res.put("orders", orderBooks);
        res.put("records", fulfilRecords);
        return res;
    }
}
