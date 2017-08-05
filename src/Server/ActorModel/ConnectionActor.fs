namespace Feezer.Server.ActorModel.Connection

open Proto
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain

type Message =
  | Connect of handleSend:(Protocol.Server->unit)
  | Disconnect
  | MessageReceived of content:Protocol.Client
  | SendMessage of message:Protocol.Server

module ConnectionActor =
  let handleMessage msg = ()

  let private behaviorsList switcher =
    let mutable handleSend = None
    let rec disconnected (ctx:IContext) =
      ctx.Message
      >>| fun (msg:Message) ->
            match msg with
            | Connect handler ->
                handleSend<-Some handler
                switcher.become connected
            | _ -> () //TODO add logging
    and connected ctx =
      ctx.Message
      >>| fun (msg:Message) ->
            match msg with
            | Disconnect ->
                handleSend<-None
                switcher.become disconnected
            | MessageReceived content -> handleMessage content
            | SendMessage message ->
                match handleSend with
                | Some handler -> handler message
                | None -> switcher.become disconnected //TODO log handler is None become disconnected
            | _ -> () //TODO add logging
    [disconnected;connected]


  let create () = behaviorOf behaviorsList