package org.neo.loyaldemo.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

import java.util.HashMap;
import java.util.Map;

@Configuration
@Data
@ConfigurationProperties(prefix="nep17")
public class ContractConfig {

    private Map<String, String> contract = new HashMap<>();
}
