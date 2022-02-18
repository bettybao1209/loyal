package org.neo.loyaldemo.chain;

import io.neow3j.protocol.core.response.NeoBlock;
import io.neow3j.protocol.core.response.Transaction;
import lombok.extern.log4j.Log4j2;
import org.neo.loyaldemo.pojo.FulfilRecord;
import org.neo.loyaldemo.pojo.OrderBook;
import org.neo.loyaldemo.service.OrderService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.math.BigInteger;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

@Component
@Log4j2
public class BlockchainListener {

    @Autowired
    CommonService commonService;

    @Autowired
    OrderService orderService;

    private static int interval = 500;

    @Scheduled(cron = "0/15 * * * * *")
    public void updateOnChainStatus() throws Exception{
        log.info("update on-chain status task begin");
        while(true){
            BigInteger remoteBLockHeight = commonService.getRemoteBlockIndex();
            log.info("#### remote block height: {}", remoteBLockHeight);
            Long syncedBlockHeight = orderService.selectBlockHeight();
            long dbBlockHeight = syncedBlockHeight == null ? 0 : syncedBlockHeight;
            long dbBlockHeightReserved = dbBlockHeight;

            if(dbBlockHeight > remoteBLockHeight.longValue()){
                log.info("Waiting for block.");
                return;
            }
            long blockDiff = remoteBLockHeight.longValue() - dbBlockHeight;
            List<OrderBook> orderBooks  = new ArrayList<>();
            List<FulfilRecord> fulfilRecords  = new ArrayList<>();
            int increment = 0;
            while(increment++ < interval && blockDiff-- > 0){
                NeoBlock block = commonService.getBlock(BigInteger.valueOf(dbBlockHeight++));
                List<Transaction> txs = block.getTransactions();
                for (Transaction transaction: txs) {
                    Map<String, Object> res = commonService.getOrderBooks(transaction, block.getTime());
                    orderBooks.addAll((List)res.getOrDefault("orders", new ArrayList<>()));
                    fulfilRecords.addAll((List)res.getOrDefault("records", new ArrayList<>()));
                }
            }
            orderService.insertOrdersAndBlock(orderBooks, fulfilRecords, dbBlockHeightReserved + increment - 1);
            log.info("#### block index persisted: {}", dbBlockHeightReserved + increment - 2);
            interval = 500;
        }
    }
}
