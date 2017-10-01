namespace Feezer.Server.ActorModel.User

open Proto
open Proto.Router
open Feezer.Server.ActorModel.FSharpApi
open Feezer.Domain
module Api = Feezer.Domain.DeezerApi

type UserActorMessage =
  | GetUserInfo

module UserActor =

  let handler counter (ctx:IContext) = ()
  let create =
    let mutable counter = 1
    counter