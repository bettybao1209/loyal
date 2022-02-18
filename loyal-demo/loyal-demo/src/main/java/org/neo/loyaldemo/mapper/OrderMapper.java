package org.neo.loyaldemo.mapper;

import org.apache.ibatis.annotations.Mapper;
import org.apache.ibatis.annotations.Param;
import org.neo.loyaldemo.pojo.FulfilRecord;
import org.neo.loyaldemo.pojo.OrderBook;

import java.util.List;

@Mapper
public interface OrderMapper {

    Long selectBlockHeight();

    void batchInsertOrders(List<OrderBook> orders);

    void batchInsertRecords(List<FulfilRecord> records);

    List<OrderBook> selectOrderByAddress(String address);

    void insertBlock(long blockHeight);

    void updateOrderAmount(List<OrderBook> orderBooks);

    void updateOrderStatus(@Param("orderId") Integer orderId);
}
