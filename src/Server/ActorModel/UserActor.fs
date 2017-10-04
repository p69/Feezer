module Feezer.Server.ActorModel.UserActor

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
open Feezer.Domain.User
module Api = Feezer.Domain.DeezerApi
open Feezer.Server.Utils

type Message =
| GetUserInfo
| HttpResponse of HttpActor.Response

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
        | Some u -> ConnectionActor.sendMessage <| Protocol.Server.UserInfo(u)
        | None ->
            let httpActor = HttpActor.createFromContext ctx
            httpActor <! HttpActor.GET(Api.User.me)
      | HttpResponse resp ->
          match resp with
          | HttpActor.Success (uri, result) ->
              if uri=Api.User.me then
                let parsedUser = fromJson<User> result
                ConnectionActor.sendMessage <| Protocol.Server.UserInfo(parsedUser)
                return! loop <| Some parsedUser
          | HttpActor.Error _ -> ()
      return! loop user
    }
  loop None

let create () = handler |> props