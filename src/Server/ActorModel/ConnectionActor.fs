module Feezer.Server.ActorModel.ConnectionActor

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



[<Literal>]
let Name = "socket"

let mutable private currentConnection:option<TypedPID<Message>> = None

let private behaviorsList connectionListeners (mailbox:Actor<IContext,Message>) =

  let connectionListeners = Router.NewBroadcastGroup(connectionListeners) |> spawnProps<ConnectionStatus>

  let rec disconnected (listeners:TypedPID<ConnectionStatus>) =
    actor {
      let! (_,msg) = mailbox.Receive()
      match msg with
      | Connect handler ->
          listeners <! Connected
          return! connected listeners handler
      | _ -> return! disconnected listeners
    }

  and connected (listeners:TypedPID<ConnectionStatus>) (handleSend:HandleSendMessage) =
    actor {
      let! (_,msg) = mailbox.Receive()
      match msg with
      | Disconnect ->
           listeners <! Disconnected
           return! disconnected listeners
      | SendMessage message ->
           handleSend message
           return! connected listeners handleSend
      | _ -> return! connected listeners handleSend //TODO: log
    }

  disconnected connectionListeners

let spawn connectionListeners =
  let actor = behaviorsList connectionListeners |> spawnNamed Name
  currentConnection<- Some(actor)
  actor

let sendMessage (msg:Protocol.Server) =
    match currentConnection with
    | Some actor -> actor <! SendMessage(msg)
    | None -> () //log