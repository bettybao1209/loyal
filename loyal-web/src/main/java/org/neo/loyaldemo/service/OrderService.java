package org.neo.loyaldemo.service;

import org.neo.loyaldemo.mapper.OrderMapper;
import org.neo.loyaldemo.pojo.FulfilRecord;
import org.neo.loyaldemo.pojo.OrderBook;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigInteger;
import java.util.ArrayList;
import java.util.List;

@Service
public class OrderService {

    @Autowired
    OrderMapper orderMapper;

    @Transactional
    public void insertOrdersAndBlock(List<OrderBook> orders, List<FulfilRecord> fulfilRecords, long blockHeight){
        if (orders.size() > 0) {
            orderMapper.batchInsertOrders(orders);
        }
        if (fulfilRecords.size() > 0) {
            orderMapper.batchInsertRecords(fulfilRecords);
        }
        List<OrderBook> orderUpdated = new ArrayList<>();
        fulfilRecords.parallelStream().forEach(fulfilRecord -> {
            OrderBook orderBook1 = new OrderBook();
            orderBook1.setOrderId(fulfilRecord.getOrderIdFrom());
            orderBook1.setFulfiledAmount(fulfilRecord.getAmount());
            OrderBook orderBook2 = new OrderBook();
            orderBook2.setOrderId(fulfilRecord.getOrderIdTo());
            orderBook2.setFulfiledAmount(fulfilRecord.getAmount());
            orderUpdated.add(orderBook1);
            orderUpdated.add(orderBook2);
        });
        if (orderUpdated.size() > 0) {
            orderMapper.updateOrderAmount(orderUpdated);
        }
        orderMapper.insertBlock(blockHeight);
    }

    public List<OrderBook> selectOrderByAddress(String address){
        return orderMapper.selectOrderByAddress(address);
    }

    public Long selectBlockHeight(){
        return orderMapper.selectBlockHeight();
    }

    public void updateOrderStatus(long orderId) {
        orderMapper.updateOrderStatus(orderId);
    }
}
