module Feezer.Server.ActorModel.FlowActor

open System
open Feezer.Server.ActorModel.FSharpApi
open Proto
module Api = Feezer.Domain.DeezerApi
open Feezer.Server.Utils
open Feezer.Domain
open Feezer.Domain.Music.Types

type Msg =
  | GetUsersFlow

let private toDomainFlow (flowJson:Api.User.FlowJson) = {
    tracks=flowJson |> List.map (fun x ->
          {
            deezerId=x.id;
            fullTitle=x.title;
            shortTitle=x.title_short;
            duration=TimeSpan.FromSeconds(x.duration |> float);
            deezerRank=x.rank;
            hasLyrics=x.explicit_lyrics;
            previewUrl=x.preview;
            url=""
          }
        )
  }

let private getFlowFromServer ctx = async {
      let http = HttpActor.createFromContext ctx
      let! result = http <?? HttpActor.GET(Api.User.flow())
      let flow =
        match result with
        | HttpActor.Success (uri, body) -> fromJson<Api.User.FlowJson> body |> toDomainFlow |> Some
        | HttpActor.Error (uri,error) -> None //logging
      return flow
    }

let private handler (mailbox:Actor<IContext, Msg>) =

  let rec notCached() = actor {
      let! (ctx,msg) = mailbox.Receive()

      match msg with
      | GetUsersFlow ->
          let flow = getFlowFromServer ctx |> Async.RunSynchronously
          match flow with
          | Some f -> return! cached mailbox f
          | None -> return! notCached()
    }

  and cached mailbox flow = actor {
        let! (ctx,msg) = mailbox.Receive()
        match msg with
        | GetUsersFlow -> Protocol.Server.UsersFlow flow |> ConnectionActor.sendMessage
    }


  notCached()

let spawn() = handler |> spawn