package deploy;

import io.neow3j.contract.ContractManagement;
import io.neow3j.contract.NefFile;
import io.neow3j.contract.SmartContract;
import io.neow3j.protocol.Neow3j;
import io.neow3j.protocol.ObjectMapperFactory;
import io.neow3j.protocol.core.response.ContractManifest;
import io.neow3j.protocol.core.response.NeoSendRawTransaction;
import io.neow3j.protocol.http.HttpService;
import io.neow3j.transaction.AccountSigner;
import io.neow3j.types.ContractParameter;
import io.neow3j.types.Hash160;
import io.neow3j.wallet.Account;

import java.io.File;
import java.io.FileInputStream;
import java.nio.file.Paths;

public class DeployOrderContract {
    public static void main(String[] args) throws Throwable{
        Neow3j neow3j = Neow3j.build(new HttpService("http://localhost:10332"));
        Account account = Account.fromWIF( "L3NtEqpbKoFmQTSmQxyDdB947dopSmKSpHaTrh3y6CHzGiPd1NM3");
        Hash160 owner = Hash160.fromAddress("NekkNKVQcMsgYoJcBNQKdWUYGt9u22vPpJ");

        File contractNefFile = Paths.get("D:\\work\\neo\\loyal\\loyal\\nef", "LoyalOrder" + ".nef").toFile();
        File contractManifestFile = Paths.get("D:\\work\\neo\\loyal\\loyal\\nef", "LoyalOrder" + ".manifest.json").toFile();

        NefFile nefFile = NefFile.readFromFile(contractNefFile);
        ContractManifest manifest;
        try (
                FileInputStream s = new FileInputStream(contractManifestFile)) {
            manifest = ObjectMapperFactory.getObjectMapper().readValue(s, ContractManifest.class);
        }

        NeoSendRawTransaction response = new ContractManagement(neow3j)
                .deploy(nefFile, manifest, ContractParameter.hash160(owner))
                .signers(AccountSigner.calledByEntry(account)
                        .setAllowedContracts(ContractManagement.SCRIPT_HASH))
                .sign()
                .send();

        Hash160 contractHash = SmartContract.calcContractHash(
                account.getScriptHash(),
                nefFile.getCheckSumAsInteger(),
                manifest.getName());
        // contract hash in big endian
        System.out.println(contractHash);
    }
}
