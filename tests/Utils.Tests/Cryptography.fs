module Tests

open Expecto
open ExpectoFsCheck
open Feezer.Utils
open System
open Tests.Common

[<AutoOpen>]
module Auto =
  let private config = GenEx.addToConfig FsCheckConfig.defaultConfig
  let testProp name = testPropertyWithConfig config name
  let ptestProp name = ptestPropertyWithConfig config name
  let ftestProp stdgen name = ftestPropertyWithConfig stdgen config name
  let password = "password"
  let salt = "saltsaltsalt"

[<Tests>]
let tests =
  testList "Encryption" [

    testProp "Encryption and decryption test" (fun (GenEx.NonEmptyString original) ->
      let encrypted = Cryptography.encrypt original password salt
      let decrypted = Cryptography.decrypt encrypted password salt
      Expect.equal original decrypted ""
    )

  ]
