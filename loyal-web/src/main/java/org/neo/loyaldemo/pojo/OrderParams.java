package org.neo.loyaldemo.pojo;

import lombok.Data;

import java.math.BigInteger;

@Data
public class OrderParams {


    private String fromSymbol;
    private String toSymbol;
    private String userAddress;
    private BigInteger amount;
    private BigInteger orderId;
}
