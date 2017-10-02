namespace Feezer.Server.ActorModel.User

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
open Feezer.Domain.User
open Feezer.Server.ActorModel.Http
open Feezer.Server.ActorModel.Connection
module Api = Feezer.Domain.DeezerApi
open Feezer.Server.Utils

type Message =
  | GetUserInfo
  | HttpResponse of Response

module UserActor =

  type JsonUser = {
    id:int;
    name:string;
    picture_small:string;
    picture_medium:string;
    picture_big:string;
    country:string;
    lang:string;
    tracklist:string;
  }

  let handler (mailbox:Actor<IContext, Message>) =
    let mutable user:option<User> = None
    let rec loop () = actor {
        let! (ctx,msg) = mailbox.Receive()
        match msg with
        | GetUserInfo ->
          match user with
          | Some u -> ClientKeeper.sendMessage <| Protocol.Server.UserInfo(u)
          | None ->
              let httpActor = HttpActor.createFromContext ctx
              httpActor <! GET(Api.User.me)

        | HttpResponse resp ->
            match resp with
            | Success (uri, result) ->
                if uri=Api.User.me then
                  let parsedUser = fromJson<User> result
                  user<-Some(parsedUser)
                  ClientKeeper.sendMessage <| Protocol.Server.UserInfo(parsedUser)
            | Error _ -> ()

        return! loop()
      }
    loop()

  let create () = handler |> spawn