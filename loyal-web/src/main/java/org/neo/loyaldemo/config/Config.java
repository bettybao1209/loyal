package org.neo.loyaldemo.config;

import io.neow3j.protocol.Neow3j;
import io.neow3j.protocol.http.HttpService;
import io.neow3j.types.Hash160;
import io.neow3j.wallet.Account;
import lombok.Data;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.HashMap;
import java.util.Map;

@Configuration
@Data
@ConfigurationProperties(prefix="user.wif")
public class Config {

    @Value("${rpc.url}")
    private String url;

    @Value("${owner.wif}")
    private String wif;

    @Value("${loyal.contract.hash}")
    private String loyalContract;

    private Map<String, String> wallet = new HashMap<>();

    @Bean
    public Account ownerAccount(){
        return Account.fromWIF(wif);
    }

    @Bean
    public Neow3j neow3j() {
        return Neow3j.build(new HttpService(url));
    }
}

