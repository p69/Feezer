namespace Feezer.Server.ActorModel.Connection

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain

type HandleSendMessage = Protocol.Server->unit

type Message =
  | Connect of handleSend:HandleSendMessage
  | Disconnect
  | SendMessage of content:Protocol.Server

type ConnectionStatus =
  | Connected
  | Disconnected

module ConnectionActor =

  [<Literal>]
  let Name = "socket"

  let private behaviorsList connectionListeners switcher =
    let listeners = Router.NewBroadcastGroup(null, connectionListeners) |> spawn
    let mutable handleSend = None

    let rec disconnected (ctx:IContext) =
      ctx.Message
      >>| fun (msg:Message) ->
            match msg with
            | Connect handler ->
                handleSend<-Some handler
                listeners <! Connected
                switcher.become connected
            | _ -> () //TODO add logging
    and connected ctx =
      ctx.Message
      >>| fun (msg:Message) ->
            match msg with
            | Disconnect ->
                handleSend<-None
                listeners <! Disconnected
                switcher.become disconnected
            | SendMessage message ->
                match handleSend with
                | Some handler -> handler message
                | None -> switcher.become disconnected //TODO log handler is None become disconnected
            | _ ->() //TODO: log
    [disconnected;connected]

  let create connectionListeners =  behaviorOf <| behaviorsList connectionListeners