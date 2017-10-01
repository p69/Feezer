namespace Feezer.Server.ActorModel.User

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
open Feezer.Domain.User
open Feezer.Server.ActorModel.Http
open Feezer.Server.ActorModel.Connection
open Chiron
module Api = Feezer.Domain.DeezerApi

type Message =
  | GetUserInfo
  | HttpResponse of Response

module UserActor =

  let handler (mailbox:Actor<IContext, Message>) =
    let mutable user:option<User> = None
    let rec loop () = actor {
        let! (ctx,msg) = mailbox.Receive()
        match msg with
        | GetUserInfo ->
          match user with
          | Some u -> ClientKeeper.sendMessage <| Protocol.Server.UserInfo({name=u.name; avatar=u.avatar})
          | None ->
              let httpActor = HttpActor.createFromContext ctx
              httpActor <! GET(Api.User.me)

        | HttpResponse resp ->
            match resp with
            | Success (uri, result) ->
                if uri=Api.User.me then
                  let userRes:JsonResult<User> = Chiron.Inference.Json.deserialize result
                  match userRes with
                  | JPass res ->
                      user<-Some(res)
                      ClientKeeper.sendMessage <| Protocol.Server.UserInfo({name=res.name; avatar=res.avatar})
                  | JFail _ -> ()
            | Error _ -> ()

        return! loop()
      }
    loop()

  let create () = handler |> spawn