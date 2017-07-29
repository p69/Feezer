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
  open Newtonsoft.Json


  let private jsonConverter = Fable.JsonConverter() :> JsonConverter
  let private fromJson<'a> value = JsonConvert.DeserializeObject<'a>(value, [|jsonConverter|])
  let private toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])

  [<EntryPoint>]
  let main argv =
      let config = Config.get()
      let clients = ResizeArray()
      let sendToAllClients msg =
         let data = toJson <| msg |> UTF8.bytes |> ByteSegment
         clients |> Seq.iter (fun (w:WebSocket) ->  w.send Text data true |> Async.Ignore |> Async.Start)
         ()

      let ws (webSocket:WebSocket) (context:HttpContext) =
        clients.Add webSocket
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
                let response =
                    match clientMessage with
                    | Authozrize ->
                        let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
                        let uri = Authorization.buildLoginUri config.DeezerAppId "http://localhost:8080/auth" permissions
                        let serverMessage = Authorization(uri)
                        toJson serverMessage |> UTF8.bytes |> ByteSegment

                do! webSocket.send Text response true

            | (Close, _, _) ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
                clients.Remove webSocket |> ignore
                loop <- false

            | _ -> ()
        }


      let  downloadString (uri:string) =
        async {
            use client = new HttpClient()
            let! response = client.GetAsync(uri) |> Async.AwaitTask
            return! response.Content.ReadAsStringAsync() |> Async.AwaitTask
        }


      let handleDeezerAuth: WebPart =
        fun (ctx:HttpContext) ->
            async {
                let codeParam = ctx.request.queryParamOpt "code"
                match codeParam with
                 | Some (_,paramValue) ->
                       match paramValue with
                        | Some code ->
                            let getAccessTokenWithParams = Authorization.getAccessToken code config.DeezerAppId config.DeezerAppSecret
                            let getAccessTokenWithJsonParse = getAccessTokenWithParams fromJson<Authorization.AuthorizationResult>
                            let getAccessTokenWothSystemDateTime = getAccessTokenWithJsonParse (fun()->DateTime.Now)

                            let! (token, expiration) = downloadString |> getAccessTokenWothSystemDateTime
                            sendToAllClients<|Authorized(expiration)
                        | None -> ()
                 | None -> ()
                return! OK "" ctx
            }

      let app =
        choose [
            path "/fcon" >=> handShake ws
            GET >=> choose
              [ path "/auth" >=> handleDeezerAuth
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
