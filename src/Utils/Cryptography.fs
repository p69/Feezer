namespace Feezer.Utils

module Cryptography =

    open System
    open System.IO
    open System.Security.Cryptography

    let mutable private passPhrase:string option = None

    let private readPassPhrase ()=
        let readFromFile fileName =
            if (not <| File.Exists fileName) then failwith "Couldn't find file with passphrase, make sure that file 'pass.phr' exists in the src/Server folder"
            File.ReadAllText fileName

        match passPhrase with
        | Some phrase -> phrase
        | None ->
            passPhrase <- Some (readFromFile <| Path.Combine(Directory.GetCurrentDirectory(), "..\\Server", "pass.phr"))
            passPhrase.Value
    let private getSalt () = Text.UTF8Encoding.UTF8.GetBytes("1dhfds12312dasd")

    let encrypt (originalString:string) =
        let originalBytes = Text.UTF8Encoding.UTF8.GetBytes(originalString)
        let pass = readPassPhrase()
        use aes = Aes.Create()
        let pdb = new Rfc2898DeriveBytes(pass, getSalt())
        use encryptor = aes.CreateEncryptor(pdb.GetBytes(32), pdb.GetBytes(16))
        use ms = new MemoryStream()
        use cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
        cryptoStream.Write(originalBytes, 0, originalBytes.Length)
        Convert.ToBase64String(ms.ToArray())

    let decrypt originalString =
        let originalBytes = Convert.FromBase64String(originalString)
        let pass = readPassPhrase()
        use aes = Aes.Create()
        let pdb = new Rfc2898DeriveBytes(pass, getSalt())
        use decryptor = aes.CreateDecryptor(pdb.GetBytes(32), pdb.GetBytes(16))
        use memoryStream = new MemoryStream(originalBytes)
        use cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
        use reader = new StreamReader(cryptoStream)
        reader.ReadToEnd()
