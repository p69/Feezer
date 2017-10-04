module Feezer.Server.ActorModel.WsRouterActor

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open System.Collections.Concurrent
open Feezer.Domain

type Routee<'a> = Protocol.Client*(Protocol.Client->'a)*TypedPID<'a>

let private router (routeesMap:ConcurrentDictionary<Protocol.Client, TypedPID<Protocol.Client>>) (mailbox:Actor<IContext,Protocol.Client>) =
    let rec loop() =
        actor {
            let! (_, msg) = mailbox.Receive()
            match routeesMap.TryGetValue msg with
            | (true, pid) -> pid <! msg
            | (fals,_) -> ()//TODO: log
            return! loop()
        }
    loop()

let create (routeesMap:ConcurrentDictionary<Protocol.Client, TypedPID<Protocol.Client>>) =
    Router.NewRoundRobinPool(router routeesMap |> props, 4)

let private handler<'a> (routes:Routee<'a> list) (mailbox:Actor<IContext,Protocol.Client>) =
    let rec loop() =
        actor {
            let! (_, msg) = mailbox.Receive()
            let choosen =
                routes
                |> List.tryPick (fun (messagePattern, mapper, actor) ->
                    if messagePattern = msg
                    then
                        let mapped = mapper msg
                        Some(actor, mapped)
                    else
                        None
                    )
            choosen |> Option.iter (fun (a,m) -> a<! m)
            return! loop()
        }
    loop()

let choose<'a> (routes:Routee<'a> list) = Router.NewRoundRobinPool(handler routes |> props, 4)
