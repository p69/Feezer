namespace Feezer.Utils

module Cryptography =

    open System
    open System.IO
    open System.Security.Cryptography

    let private stringToBytes str =
        Convert.FromBase64String str

    let private bytesToString bytes =
        Convert.ToBase64String bytes

    let private getAesKeyAndIV password salt (algorithm:SymmetricAlgorithm) =
        let iv = Array.create 16 (0|>byte)
        let key = Array.create 16 (0|>byte)
        let deriveBytes = new Rfc2898DeriveBytes(password, stringToBytes salt)
        (deriveBytes.GetBytes(algorithm.KeySize/8), deriveBytes.GetBytes(algorithm.BlockSize/8))

    let private writeToCryptoStream encryptor (originalString:string) =
        use ms = new MemoryStream()
        use cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
        use writer = new StreamWriter(cryptoStream)
        writer.Write originalString
        writer.Flush()
        cryptoStream.FlushFinalBlock()
        ms.ToArray()

    let private readFromCryptoStream decryptor (cipher:byte array) =
        use ms = new MemoryStream(cipher)
        use cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)
        use reader = new StreamReader(cryptoStream)
        reader.ReadToEnd()

    let encrypt (originalString:string) (password:string) salt =
        if String.IsNullOrWhiteSpace originalString then "" else
        use aes = Aes.Create()
        let (key, iv) = getAesKeyAndIV password salt aes
        use encryptor = aes.CreateEncryptor(key, iv)
        let encryptedContent = writeToCryptoStream encryptor originalString
        bytesToString encryptedContent

    let decrypt (encryptedText:string) (password:string) salt =
        if String.IsNullOrWhiteSpace encryptedText then "" else
        use aes = Aes.Create()
        let (key, iv) = getAesKeyAndIV password salt aes
        use decryptor = aes.CreateDecryptor(key, iv)
        let cipher = stringToBytes encryptedText
        readFromCryptoStream decryptor cipher