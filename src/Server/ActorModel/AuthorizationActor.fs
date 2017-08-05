namespace Feezer.Server.ActorModel.Authorization

open Proto
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain

module Authorization =

  let create (connectionActor:PID) =
    actor{
        receive ignore
    }