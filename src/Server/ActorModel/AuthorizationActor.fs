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

type AuthFlow =
    | CodeCallbackReceived of code:string

module AuthorizationActor =

  [<Literal>]
  let Name = "auth"

  type AuthorizationResult = {
        access_token:string;
        expires:int
    }

  let handleReceive config (ctx:IContext) =
    let mutable token = None
    ctx.Message
    >>= fun (msg:Protocol.Client) ->
          match msg with
          | Protocol.Authozrize ->
              let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
              let uri = Authorization.buildLoginUri config.DeezerAppId "http://localhost:8080/auth" permissions
              ClientKeeper.sendMessage <| Protocol.Authorization(uri)
    >>= fun (msg:AuthFlow) ->
          match msg with
          | CodeCallbackReceived code ->
              let tokenUri = Authorization.buildTokenUri config.DeezerAppId config.DeezerAppSecret code
              let httpActor = HttpActor.create() |> spawnFromContext ctx
              httpActor <? (GET(tokenUri), ctx.Self)
    >>| fun (msg:Response) ->
          match msg with
          | Success (_,result) ->
              let result = fromJson result
              let expiration =
                match result.expires with
                | 0 -> Protocol.Never
                | seconds -> Protocol.Date(DateTime.Now.AddSeconds(seconds|>float))
              ClientKeeper.sendMessage <| Protocol.Authorized(expiration)
              token <- Some result.access_token
              //TODO save token in DB
          | _ -> () //TODO handle errors


  let create (config:AppConfig) =
    actor{
        receive (handleReceive config)
    }