package org.neo.loyaldemo.pojo;

import lombok.Data;

import java.math.BigInteger;

@Data
public class OrderBook {

    private BigInteger orderId;
    private String userAddress;
    private String fromAssetAddress;
    private String toAssetAddress;
    private BigInteger amount;
    private long createTime;
    private String txHash;
    private BigInteger fulfiledAmount;
}
