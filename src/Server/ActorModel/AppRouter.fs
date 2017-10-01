namespace Feezer.Server.ActorModel.AppRouter

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open System.Collections.Concurrent
open Feezer.Domain

module RouterActor =

    let private router (routeesMap:ConcurrentDictionary<Protocol.Client, PID>) (mailbox:Actor<IContext,Protocol.Client>) =
        let rec loop() =
            actor {
                let! (_, msg) = mailbox.Receive()
                match routeesMap.TryGetValue msg with
                | (true, pid) -> pid <!! msg
                | (fals,_) -> ()//TODO: log
                return! loop()
            }
        loop()

    let create (routeesMap:ConcurrentDictionary<Protocol.Client, PID>) =
        Router.NewRoundRobinPool(router routeesMap |> props, 4)