module Tests

open Expecto
open ExpectoFsCheck
open Feezer.Domain.User
open FsCheck
open System

module Gen =
    type Float01 = Float01 of float
    let float01Arb =
        let maxValue = float UInt32.MaxValue
        Arb.convert
            (fun (DoNotSize a) -> float a / maxValue |> Float01)
            (fun (Float01 f) -> f * maxValue + 0.5 |> uint64 |> DoNotSize)
            Arb.from
    type 'a ListOf100 = ListOf100 of 'a list
    let listOf100Arb() =
        Gen.listOfLength 100 Arb.generate
        |> Arb.fromGen
        |> Arb.convert ListOf100 (fun (ListOf100 l) -> l)
    type 'a ListOfAtLeast2 = ListOfAtLeast2 of 'a list
    let listOfAtLeast2Arb() =
        Arb.convert
            (fun (h1,h2,t) -> ListOfAtLeast2 (h1::h2::t))
            (function
                | ListOfAtLeast2 (h1::h2::t) -> h1,h2,t
                | e -> failwithf "not possible in listOfAtLeast2Arb: %A" e)
            Arb.from

    type 'a ListOfAtLeast1 = ListOfAtLeast1 of 'a list
    let listOfAtLeast1Arb() =
        Arb.convert
            (fun (h1,t) -> ListOfAtLeast1 (h1::t))
            (function
                | ListOfAtLeast1 (h1::t) -> h1,t
                | e -> failwithf "not possible in listOfAtLeast2Arb: %A" e)
            Arb.from
    let addToConfig config =
        {config with arbitrary = typeof<Float01>.DeclaringType::config.arbitrary}
[<AutoOpen>]
module Auto =
    let private config = Gen.addToConfig FsCheckConfig.defaultConfig
    let testProp name = testPropertyWithConfig config name
    let ptestProp name = ptestPropertyWithConfig config name
    let ftestProp stdgen name = ftestPropertyWithConfig stdgen config name

    let appId = "123"
    let authCallbackUrl = "http://localhost:8080/auth"

[<Tests>]
let tests =
  testList "Authorization" [

    testProp "stringify permissions" (fun (Gen.ListOfAtLeast1 permissions) ->
      let composition = permissions |> List.fold (<||>) (Authorization.PermissionComposition(None))
      let expectedString = permissions |> List.map (fun item -> item.Value) |> List.toSeq
      Expect.containsAll expectedString (composition.AsQueryString.Split(',') |> Array.toSeq) ""
    )

    testCase "crete uri for authorization" <| fun _ ->
      let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
      let uri = Authorization.buildLoginUri appId authCallbackUrl permissions
      let expectedUri = Authorization.deezerAuthUri+"app_id="+appId+"&redirect_uri="+authCallbackUrl+"&perms="+permissions.AsQueryString
      Expect.equal expectedUri uri ""
  ]
