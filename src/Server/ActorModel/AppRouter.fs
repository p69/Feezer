namespace Feezer.Server.ActorModel.AppRouter

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open System.Collections.Concurrent
open Feezer.Domain

module RouterActor =
    let handleReceive (routeesMap:ConcurrentDictionary<Protocol.Client, PID>) (ctx:IContext) =
      ctx.Message
      >>| fun (msg:Protocol.Client) ->
          match routeesMap.TryGetValue msg with
          | (true, pid) -> pid <! msg
          | (fals,_) -> ()//TODO: log
    let create (routeesMap:ConcurrentDictionary<Protocol.Client, PID>) =
        let routeeProps = actor {
                receive (handleReceive routeesMap)
            }
        Router.NewRoundRobinPool(routeeProps, 4)