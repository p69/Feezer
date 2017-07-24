namespace Feezer.Utils

module Cryptography =

    open System
    open System.IO
    open System.Security.Cryptography
    let private getSalt () = Text.UTF8Encoding.UTF8.GetBytes("1dhfds12312dasd")

    let encrypt (originalString:string) passPhrase =
        let originalBytes = Text.UTF8Encoding.UTF8.GetBytes(originalString)
        use aes = Aes.Create()
        let pdb = new Rfc2898DeriveBytes(passPhrase, getSalt())
        use encryptor = aes.CreateEncryptor(pdb.GetBytes(32), pdb.GetBytes(16))
        use ms = new MemoryStream()
        use cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
        cryptoStream.Write(originalBytes, 0, originalBytes.Length)
        Convert.ToBase64String(ms.ToArray())

    let decrypt originalString passPhrase =
        let originalBytes = Convert.FromBase64String(originalString)
        use aes = Aes.Create()
        let pdb = new Rfc2898DeriveBytes(passPhrase, getSalt())
        use decryptor = aes.CreateDecryptor(pdb.GetBytes(32), pdb.GetBytes(16))
        use memoryStream = new MemoryStream(originalBytes)
        use cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
        use reader = new StreamReader(cryptoStream)
        reader.ReadToEnd()