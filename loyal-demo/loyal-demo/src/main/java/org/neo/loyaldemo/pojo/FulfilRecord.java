package org.neo.loyaldemo.pojo;

import lombok.Data;

import java.math.BigInteger;

@Data
public class FulfilRecord {

    private BigInteger orderIdFrom;
    private BigInteger orderIdTo;
    private BigInteger amount;
    private String txHash;
    private long createTime;
}
