namespace Feezer.Server

[<AutoOpen>]
module Server =

  open System
  open System.Threading
  open System.Net.Http
  open Feezer.Domain.Protocol
  open Feezer.Domain.User
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.Successful
  open Suave.WebSocket
  open Suave.Sockets
  open Suave.Sockets.Control
  open Feezer.Server.Utils
  open Proto
  open Feezer.Server.ActorModel.Connection
  open Feezer.Server.ActorModel.FSharpApi
  open Feezer.Server.ActorModel.Authorization
  open Feezer.Server.ActorModel.AppRouter
  open System.Collections.Concurrent

  [<EntryPoint>]
  let main argv =
      let config = Config.get()
      let send (w:WebSocket) msg =
         let data = toJson <| msg |> UTF8.bytes |> ByteSegment
         w.send Text data true |> Async.Ignore |> Async.Start
         ()

      let authorizationActor = (AuthorizationActor.create config) |> spawnNamed AuthorizationActor.Name

      let appRouter = RouterActor.create <| ConcurrentDictionary<Client,PID>(dict [(Client.Authozrize, authorizationActor)]) |> spawn
      let connectionActor = ConnectionActor.create [|authorizationActor|] |> spawnNamed ConnectionActor.Name
      ClientKeeper.initBy connectionActor

      let ws (webSocket:WebSocket) (context:HttpContext) =
        connectionActor <! Connect (send webSocket)
        socket {
          let mutable loop = true
          while loop do
            let! msg = webSocket.read()

            match msg with
            | (Text, data, true) ->
                let strData = UTF8.toString data
                let evt = Logging.Message.eventX <| "Message received: " + strData
                let clientMessage = fromJson<Client> strData
                context.runtime.logger.log Logging.Info evt |> Async.Start
                appRouter <! clientMessage
            | (Close, _, _) ->
                connectionActor <! Disconnect
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
                loop <- false

            | _ -> ()
        }

      let handleDeezerAuth: WebPart =
        fun (ctx:HttpContext) ->
            async {
                let codeParam = ctx.request.queryParamOpt Authorization.authCodeParamName
                match codeParam with
                 | Some (_,paramValue) ->
                       match paramValue with
                        | Some code -> authorizationActor <! CodeCallbackReceived(code)
                        | None -> ()
                 | None -> ()
                return! OK "" ctx
            }

      let app =
        choose [
            path "/fcon" >=> handShake ws
            GET >=> choose
              [ path "/auth" >=> handleDeezerAuth
              ]
            POST >=> choose
              [ path "/post" >=> OK "dasda"]
          ]

      let cts = new CancellationTokenSource()
      let conf = { defaultConfig with cancellationToken = cts.Token }
      let listening, server = startWebServerAsync conf app

      Async.Start(server, cts.Token)
      printfn "Feezer server is started..."
      Console.ReadKey true |> ignore
      printfn "Feezer server is stopped"
      cts.Cancel()

      0 // return an integer exit code
