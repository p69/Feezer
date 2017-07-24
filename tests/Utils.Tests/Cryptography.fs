module Tests

open Expecto
open ExpectoFsCheck
open Feezer.Utils
open FsCheck
open System

[<AutoOpen>]
module Auto =
    let passPhrase = "test"    

[<Tests>]
let tests =
  testList "Encryption" [

    testProperty "Encryption and decryption test" <| fun original ->
      let encrypted = Cryptography.encrypt original passPhrase
      let decrypted = Cryptography.decrypt encrypted passPhrase
      Expect.equal original decrypted ""
   
  ]
