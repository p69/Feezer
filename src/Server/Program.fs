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
  open Suave.Sockets.Control
  open Feezer.Server.Utils
  open Proto
  open Feezer.Server.ActorModel
  open Feezer.Server.ActorModel.FSharpApi
  open System.Collections.Concurrent
  open System.Text

  [<EntryPoint>]
  let main argv =
      let config = Config.get()
      let send (w:WebSocket) msg =
         let data = msg |> toJson |> Encoding.UTF8.GetBytes |> ArraySegment
         w.send Text data true |> Async.Ignore |> Async.Start
         ()

      let userActor = (UserActor.create config) |> spawnProps<UserActor.Message>
      let flowActor = FlowActor.spawn()

      let wsRouter =
          WsRouterActor.choose [
              (Client.Authorize, (fun x->UserActor.Client(x)), userActor)
              (Client.GetUser, (fun x->UserActor.Client(x)), userActor)
              (Client.LoadFlow, (fun _->FlowActor.GetUsersFlow), flowActor)
          ] |> spawnProps

      let connectionActor = ConnectionActor.spawn [|userActor.Origin|]

      let ws (webSocket:WebSocket) (context:HttpContext) =
        connectionActor <! ConnectionActor.Connect (send webSocket)
        socket {
          let mutable loop = true
          while loop do
            let! msg = webSocket.read()

            match msg with
            | (Text, data, true) ->
                let strData = Encoding.UTF8.GetString data
                let evt = Logging.Message.eventX <| "Message received: " + strData
                let clientMessage = fromJson<Client> strData
                context.runtime.logger.log Logging.Info evt |> Async.Start
                wsRouter <! clientMessage
            | (Close, _, _) ->
                connectionActor <! ConnectionActor.Disconnect
                let emptyResponse = [||] |> ArraySegment
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
                        | Some code -> userActor <! (UserActor.AuthFlow <| UserActor.CodeCallbackReceived(code))
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