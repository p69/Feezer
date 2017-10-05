module Feezer.Server.ActorModel.AuthorizationActor

open Proto
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
open Feezer.Domain.User
open Feezer.Server.Config
open Feezer.Server.Utils
open System
module Api = Feezer.Domain.DeezerApi


type AuthFlow =
    | CodeCallbackReceived of code:string

type Message =
    | AuthFlow of AuthFlow
    | Client of Protocol.Client


[<Literal>]
let Name = "auth"

type AuthorizationResultJson = {
      access_token:string;
      expires:int
  }

let private handleReceive config (mailbox:Actor<IContext, Message>)=
  let rec loop() =
      actor {
          let! (ctx, msg) = mailbox.Receive()
          match msg with
          | Client clt ->
              match clt with
              | Protocol.Authozrize ->
                  let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
                  let uri = Authorization.buildLoginUri config.DeezerAppId "http://localhost:8080/auth" permissions
                  ConnectionActor.sendMessage <| Protocol.Authorization(uri)
          | AuthFlow af ->
              match af with
              | CodeCallbackReceived code ->
                    async {
                        let tokenUri = Authorization.buildTokenUri config.DeezerAppId config.DeezerAppSecret code
                        let httpActor = HttpActor.createFromContext ctx
                        let! hr = httpActor <?? HttpActor.GET(tokenUri)
                        match hr with
                        | HttpActor.Success (_,result) ->
                            let result = fromJson result
                            let expiration =
                              match result.expires with
                              | 0 -> Protocol.Never
                              | seconds -> Protocol.Date(DateTime.Now.AddSeconds(seconds|>float))
                            ConnectionActor.sendMessage <| Protocol.Authorized(expiration)
                            Api.setToken result.access_token
                            let userActor = UserActor.create() |> spawnPropsFromContext<UserActor.Message> ctx
                            userActor <! UserActor.GetUserInfo
                        //TODO save token in DB
                        | _ -> () //TODO handle errors
                    } |> Async.RunSynchronously |> ignore

          return! loop()
      }
  loop()

let create (config:AppConfig) =
  handleReceive config |> props