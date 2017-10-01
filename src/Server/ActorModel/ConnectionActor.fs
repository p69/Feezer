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

  let private behaviorsList connectionListeners (mailbox:Actor<IContext,Message>) =
    let listeners = Router.NewBroadcastGroup(connectionListeners) |> spawnProps
    let mutable handleSend = None

    let rec disconnected () =
      actor {
        let! (_,msg) = mailbox.Receive()
        match msg with
        | Connect handler ->
            handleSend<-Some handler
            listeners <! Connected
            return! connected()
        | _ -> return! disconnected()
      }

    and connected () =
      actor {
        let! (_,msg) = mailbox.Receive()
        match msg with
        | Disconnect ->
             handleSend<-None
             listeners <! Disconnected
             return! disconnected()
        | SendMessage message ->
             match handleSend with
             | Some handler ->
                 handler message
                 return! connected()
             | None -> return! disconnected() //TODO log handler is None become disconnected
        | _ -> return! connected() //TODO: log
      }

    disconnected()

  let create connectionListeners =  props <| behaviorsList connectionListeners