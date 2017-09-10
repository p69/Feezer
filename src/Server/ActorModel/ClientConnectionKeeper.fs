namespace Feezer.Server.ActorModel.Connection

open Proto
open ConnectionActor
open Feezer.Domain
open Feezer.Server.ActorModel.FSharpApi

module ClientKeeper =
    let mutable private clientConnection:option<PID> = None
    let initBy actor =
        clientConnection<-Some(actor)

    let sendMessage (msg:Protocol.Server) =
        match clientConnection with
        | Some actor -> actor <! msg
        | None -> () //log