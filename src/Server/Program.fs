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

  [<EntryPoint>]
  let main argv =
      let permissions = Authorization.Email <||> Authorization.Basic <||> Authorization.DeleteLibrary <||> Authorization.ManageLibrary <||> Authorization.OfflineAccess
      let uri = Authorization.buildLoginUri "app_id" "http://localhost:8080/auth" permissions

      let app =
        choose
          [
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
