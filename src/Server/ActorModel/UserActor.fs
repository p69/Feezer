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

module UserActor =
  type Message =
  | GetUserInfo
  | HttpResponse of Response

  type private JsonUser = {
    id:int;
    name:string;
    picture_small:string;
    picture_medium:string;
    picture_big:string;
    country:string;
    lang:string;
    tracklist:string;
  }

  let private handler (mailbox:Actor<IContext, Message>) =

    let rec loop (user:option<User>) = actor {
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
                  ClientKeeper.sendMessage <| Protocol.Server.UserInfo(parsedUser)
                  return! loop <| Some parsedUser
            | Error _ -> ()

        return! loop user
      }
    loop None

  let create () = handler |> props