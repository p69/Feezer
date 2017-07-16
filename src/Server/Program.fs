namespace Feezer.Server

[<AutoOpen>]
module Server =

  open System
  open System.Threading
  open Feezer.Domain.User
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.Successful
  open Suave.WebSocket
  open Suave.Sockets
  open Suave.Sockets.Control

  [<EntryPoint>]
  let main argv =
      let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
      let uri = Authorization.buildLoginUri "app_id" "http://localhost:8080/auth" permissions

      let ws (webSocket:WebSocket) (context:HttpContext) =
        socket {
          let mutable loop = true

          while loop do
            let! msg = webSocket.read()

            match msg with
            | (Text, data, true) ->
                let str = UTF8.toString data
                let response = sprintf "response to %s" str
                let byteResponse =
                    response
                    |> System.Text.Encoding.ASCII.GetBytes
                    |> ByteSegment
                do! webSocket.send Text byteResponse true

            | (Close, _, _) ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
                loop <- false

            | _ -> ()
        }

      let app =
        choose [
            path "/websocket" >=> handShake ws
            GET >=> choose
              [ path "/hello" >=> OK "hi"
                path "/bye" >=> OK "good bye"]
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
