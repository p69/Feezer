namespace Feezer.Server.ActorModel.Authorization

open Proto
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
open Feezer.Domain.User
open Feezer.Server.Config
open Feezer.Server.Utils
open Feezer.Server.ActorModel.Http
open Feezer.Server.ActorModel.Connection
open System
open Feezer.Server.ActorModel.User

module Api = Feezer.Domain.DeezerApi


type AuthFlow =
    | CodeCallbackReceived of code:string

type Message =
    | AuthFlow of AuthFlow
    | Client of Protocol.Client
    | HttpResponse of Response

module AuthorizationActor =

  [<Literal>]
  let Name = "auth"

  type AuthorizationResult = {
        access_token:string;
        expires:int
    }

  let handleReceive config (mailbox:Actor<IContext, Message>)=
    let rec loop() =
        actor {
            let! (ctx, msg) = mailbox.Receive()
            match msg with
            | Client clt ->
                match clt with
                | Protocol.Authozrize ->
                    let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
                    let uri = Authorization.buildLoginUri config.DeezerAppId "http://localhost:8080/auth" permissions
                    ClientKeeper.sendMessage <| Protocol.Authorization(uri)
            | AuthFlow af ->
                match af with
                | CodeCallbackReceived code ->
                      let tokenUri = Authorization.buildTokenUri config.DeezerAppId config.DeezerAppSecret code
                      let httpActor = HttpActor.createFromContext ctx
                      httpActor <? (GET(tokenUri), ctx.Self)
            | HttpResponse hr ->
                match hr with
                | Success (_,result) ->
                      let result = fromJson result
                      let expiration =
                        match result.expires with
                        | 0 -> Protocol.Never
                        | seconds -> Protocol.Date(DateTime.Now.AddSeconds(seconds|>float))
                      ClientKeeper.sendMessage <| Protocol.Authorized(expiration)
                      Api.setToken result.access_token
                      let userActor = UserActor.create() |> spawnPropsFromContext<UserActor.Message> ctx
                      userActor <! UserActor.GetUserInfo
                      //TODO save token in DB
                | _ -> () //TODO handle errors
            return! loop()
        }
    loop()


  let create (config:AppConfig) =
    handleReceive config |> props