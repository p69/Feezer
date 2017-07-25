module Tests

open Expecto
open ExpectoFsCheck
open Feezer.Domain.User
open System
open Tests.Common

[<AutoOpen>]
module Auto =
    let private config = GenEx.addToConfig FsCheckConfig.defaultConfig
    let testProp name = testPropertyWithConfig config name
    let ptestProp name = ptestPropertyWithConfig config name
    let ftestProp stdgen name = ftestPropertyWithConfig stdgen config name

    let appId = "123"
    let authCallbackUrl = "http://localhost:8080/auth"

[<Tests>]
let tests =
  testList "Authorization" [

    testProp "stringify permissions" (fun (GenEx.ListOfAtLeast1 permissions) ->
      let composition = permissions |> List.fold (<||>) (Authorization.PermissionComposition(None))
      let expectedString = permissions |> List.map (fun item -> item.Value) |> List.toSeq
      Expect.containsAll expectedString (composition.AsQueryString.Split(',') |> Array.toSeq) ""
    )

    testCase "crete uri for authorization" <| fun _ ->
      let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
      let uri = Authorization.buildLoginUri appId authCallbackUrl permissions
      let expectedUri = Authorization.deezerBaseAuthUri+"app_id="+appId+"&redirect_uri="+authCallbackUrl+"&perms="+permissions.AsQueryString
      Expect.equal expectedUri uri ""
  ]
